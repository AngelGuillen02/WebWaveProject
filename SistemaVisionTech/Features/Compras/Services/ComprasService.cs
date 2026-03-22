using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Dtos.Compras;
using SistemaVisionTech.Features.Compras.Dtos.Pagos;
using SistemaVisionTech.Features.Compras.Enums;
using SistemaVisionTech.Features.Compras.Interfeces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using Entities = SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Compras.Services
{
    public class ComprasService : IComprasService
    {
        private readonly WebWaveDbContext _context;

        public ComprasService(WebWaveDbContext context)
        {
            _context = context;
        }


        public async Task<Result<IEnumerable<CompraDto>>> ObtenerComprasAsync()
        {
            var compras = await _context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.EstadoCompra)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(c => c.Pagos)
                    .ThenInclude(p => p.MetodoPago)
                .OrderByDescending(c => c.FechaCompra)
                .Select(c => MapearCompraResponse(c))
                .ToListAsync();

            return Result<IEnumerable<CompraDto>>.Ok(compras);
        }

        public async Task<Result<CompraDto>> ObtenerCompraPorIdAsync(int compraId)
        {
            var compra = await _context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.EstadoCompra)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(c => c.Pagos)
                    .ThenInclude(p => p.MetodoPago)
                .FirstOrDefaultAsync(c => c.CompraId == compraId);

            if (compra is null)
                return Result<CompraDto>.Fail(
                    $"La compra con Id {compraId} no encontrada.");

            return Result<CompraDto>.Ok(MapearCompraResponse(compra));
        }

        // ─── CREAR COMPRA ────────────────────────────────────────────────

        public async Task<Result<CompraDto>> CrearCompraAsync(CrearCompraDto dto)
        {
            if (dto.Detalles is null || dto.Detalles.Count == 0)
                return Result<CompraDto>.Fail(
                    "La compra debe tener al menos un producto.", isValidation: true);

            if (dto.Detalles.Any(d => d.Cantidad <= 0))
                return Result<CompraDto>.Fail(
                    "La cantidad de cada producto debe ser mayor a cero.", isValidation: true);

            if (dto.Detalles.Any(d => d.PrecioUnitario <= 0))
                return Result<CompraDto>.Fail(
                    "El precio unitario de cada producto debe ser mayor a cero.", isValidation: true);

            if (dto.Detalles.GroupBy(d => d.ProductoId).Any(g => g.Count() > 1))
                return Result<CompraDto>.Fail(
                    "No se puede repetir el mismo producto en los detalles.", isValidation: true);

            var proveedorExiste = await _context.Proveedores
                .AnyAsync(p => p.ProveedorId == dto.ProveedorId);

            if (!proveedorExiste)
                return Result<CompraDto>.Fail(
                    $"El proveedor con Id {dto.ProveedorId} no existe.");

            var productosIds = dto.Detalles
                .Select(d => d.ProductoId)
                .ToList();

            var productosExistentes = await _context.Productos
                .Where(p => productosIds.Contains(p.ProductoId))
                .Select(p => p.ProductoId)
                .ToListAsync();

            var productosNoEncontrados = productosIds
                .Except(productosExistentes)
                .ToList();

            if (productosNoEncontrados.Count != 0)
                return Result<CompraDto>.Fail(
                    $"Los siguientes productos no existen: " +
                    $"{string.Join(", ", productosNoEncontrados)}.");

            var detallesEntidad = dto.Detalles.Select(d => new ComprasDetalles
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Total = d.PrecioUnitario * d.Cantidad
            }).ToList();

            var compra = new Entities.Compras
            {
                ProveedorId = dto.ProveedorId,
                FechaCompra = DateTime.Now,
                Total = detallesEntidad.Sum(d => d.Total),
                EstadoCompraId = (int)EstadoCompraEnum.Pendiente,
                Detalles = detallesEntidad
            };

            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();

            return await ObtenerCompraPorIdAsync(compra.CompraId);
        }

        // ─── RECIBIR COMPRA ──────────────────────────────────────────────

        public async Task<Result<CompraDto>> RecibirCompraAsync(int compraId)
        {
            var compra = await _context.Compras
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CompraId == compraId);

            if (compra is null)
                return Result<CompraDto>.Fail(
                    $"La compra con Id {compraId} no existe.");

            if (compra.EstadoCompraId != (int)EstadoCompraEnum.Pendiente)
                return Result<CompraDto>.Fail(
                    "Solo se pueden recibir compras en estado Pendiente.");

            var productosIds = compra.Detalles
                .Select(d => d.ProductoId)
                .ToList();

            var inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            foreach (var detalle in compra.Detalles)
            {
                var inv = inventarios
                    .FirstOrDefault(i => i.ProductoId == detalle.ProductoId);

                if (inv is null)
                {
                    inv = new Entities.Inventario
                    {
                        ProductoId = detalle.ProductoId,
                        Cantidad = 0,
                        FechaIngreso = DateTime.Now
                    };
                    _context.Inventario.Add(inv);
                    await _context.SaveChangesAsync();
                }

                inv.Cantidad += detalle.Cantidad;

                _context.HistorialMovimientoInventario.Add(
                    new HistorialMovimientoInventario
                    {
                        InventarioId = inv.InventarioId,
                        Cantidad = detalle.Cantidad,
                        TipoMovimiento = "ENTRADA",
                        FechaMovimiento = DateTime.Now
                    });
            }

            compra.EstadoCompraId = (int)EstadoCompraEnum.Recibida;
            await _context.SaveChangesAsync();

            return await ObtenerCompraPorIdAsync(compraId);
        }

        // ─── ANULAR COMPRA ───────────────────────────────────────────────

        public async Task<Result<CompraDto>> AnularCompraAsync(int compraId)
        {
            var compra = await _context.Compras
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CompraId == compraId);

            if (compra is null)
                return Result<CompraDto>.Fail(
                    $"La compra con Id {compraId} no existe.");

            if (compra.EstadoCompraId == (int)EstadoCompraEnum.Anulada)
                return Result<CompraDto>.Fail(
                    "La compra ya se encuentra anulada.");

            // Solo revertir stock si la compra ya fue recibida
            if (compra.EstadoCompraId == (int)EstadoCompraEnum.Recibida)
            {
                var productosIds = compra.Detalles
                    .Select(d => d.ProductoId)
                    .ToList();

                var inventarios = await _context.Inventario
                    .Where(i => productosIds.Contains(i.ProductoId))
                    .ToListAsync();

                foreach (var detalle in compra.Detalles)
                {
                    var inv = inventarios
                        .FirstOrDefault(i => i.ProductoId == detalle.ProductoId);

                    if (inv is null || inv.Cantidad < detalle.Cantidad)
                    {
                        var disponible = inv?.Cantidad ?? 0;
                        return Result<CompraDto>.Fail(
                            $"No se puede anular la compra. " +
                            $"El producto Id {detalle.ProductoId} solo tiene " +
                            $"{disponible} unidades en inventario y se intentan " +
                            $"retirar {detalle.Cantidad}.");
                    }

                    inv.Cantidad -= detalle.Cantidad;

                    _context.HistorialMovimientoInventario.Add(
                        new HistorialMovimientoInventario
                        {
                            InventarioId = inv.InventarioId,
                            Cantidad = detalle.Cantidad,
                            TipoMovimiento = "SALIDA",
                            FechaMovimiento = DateTime.Now
                        });
                }
            }

            compra.EstadoCompraId = (int)EstadoCompraEnum.Anulada;
            await _context.SaveChangesAsync();

            return await ObtenerCompraPorIdAsync(compraId);
        }

        // ─── REGISTRAR PAGO ──────────────────────────────────────────────

        public async Task<Result<PagoCompraDto>> RegistrarPagoAsync(CrearPagoCompraDto dto)
        {
            if (dto.Monto <= 0)
                return Result<PagoCompraDto>.Fail(
                    "El monto del pago debe ser mayor a cero.", isValidation: true);

            var compra = await _context.Compras
                .Include(c => c.Pagos)
                .FirstOrDefaultAsync(c => c.CompraId == dto.CompraId);

            if (compra is null)
                return Result<PagoCompraDto>.Fail(
                    $"La compra con Id {dto.CompraId} no existe.");

            if (compra.EstadoCompraId == (int)EstadoCompraEnum.Anulada)
                return Result<PagoCompraDto>.Fail(
                    "No se puede registrar pago a una compra anulada.");

            var totalPagado = compra.Pagos.Sum(p => p.Monto);
            var pendientePago = compra.Total - totalPagado;

            if (dto.Monto > pendientePago)
                return Result<PagoCompraDto>.Fail(
                    $"El monto excede el saldo pendiente. " +
                    $"Pendiente: {pendientePago:C}, " +
                    $"Monto enviado: {dto.Monto:C}.");

            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.MetodoPagoId == dto.MetodoPagoId);

            if (!metodoPagoExiste)
                return Result<PagoCompraDto>.Fail(
                    $"El método de pago con Id {dto.MetodoPagoId} no existe.");

            var pago = new PagosCompra
            {
                CompraId = dto.CompraId,
                MetodoPagoId = dto.MetodoPagoId,
                Monto = dto.Monto,
                FechaPago = DateTime.Now
            };

            _context.PagosCompra.Add(pago);
            await _context.SaveChangesAsync();

            var nombreMetodo = await _context.MetodosPago
                .Where(m => m.MetodoPagoId == dto.MetodoPagoId)
                .Select(m => m.Nombre)
                .FirstAsync();

            return Result<PagoCompraDto>.Ok(new PagoCompraDto
            {
                PagoCompraId = pago.PagoCompraId,
                CompraId = pago.CompraId,
                MetodoPago = nombreMetodo,
                Monto = pago.Monto,
                FechaPago = pago.FechaPago
            });
        }

        private static CompraDto MapearCompraResponse(Entities.Compras c)
        {
            return new CompraDto
            {
                CompraId = c.CompraId,
                ProveedorId = c.ProveedorId,
                Proveedor = c.Proveedor.Nombre,
                FechaCompra = c.FechaCompra,
                Total = c.Total,
                EstadoCompra = c.EstadoCompra.Nombre,
                Detalles = c.Detalles.Select(d => new CompraDetalleDto
                {
                    CompraDetalleId = d.CompraDetalleId,
                    ProductoId = d.ProductoId,
                    Producto = d.Producto.Nombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Total = d.Total
                }).ToList(),
                Pagos = c.Pagos.Select(p => new PagoCompraDto
                {
                    PagoCompraId = p.PagoCompraId,
                    CompraId = p.CompraId,
                    MetodoPago = p.MetodoPago.Nombre,
                    Monto = p.Monto,
                    FechaPago = p.FechaPago
                }).ToList()
            };
        }
    }
}