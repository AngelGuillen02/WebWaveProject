using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos.Compras;
using SistemaVisionTech.Features.Compras.Dtos.Pagos;
using SistemaVisionTech.Features.Compras.Enums;
using SistemaVisionTech.Features.Compras.Interfaces;
using SistemaVisionTech.Features.Inventario.Enums;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

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
            List<CompraDto> compras = await _context.Compras
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
            Compra? compra = await _context.Compras
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

            bool proveedorExiste = await _context.Proveedores
                .AnyAsync(p => p.ProveedorId == dto.ProveedorId);

            if (!proveedorExiste)
                return Result<CompraDto>.Fail(
                    $"El proveedor con Id {dto.ProveedorId} no existe.");

            List<int>? productosIds = dto.Detalles
                .Select(d => d.ProductoId)
                .ToList();

            List<int>? productosExistentes = await _context.Productos
                .Where(p => productosIds.Contains(p.ProductoId))
                .Select(p => p.ProductoId)
                .ToListAsync();

            List<int>? productosNoEncontrados = productosIds
                .Except(productosExistentes)
                .ToList();

            if (productosNoEncontrados.Count != 0)
                return Result<CompraDto>.Fail(
                    $"Los siguientes productos no existen: " +
                    $"{string.Join(", ", productosNoEncontrados)}.");

            List<ComprasDetalles>? detallesEntidad = dto.Detalles.Select(d => new ComprasDetalles
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Total = d.PrecioUnitario * d.Cantidad
            }).ToList();

            Compra compra = new()
            {
                ProveedorId = dto.ProveedorId,
                FechaCompra = DateTime.UtcNow,
                Total = detallesEntidad.Sum(d => d.Total),
                EstadoCompraId = (int)EstadoCompraEnum.Pendiente,
                Detalles = detallesEntidad
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await ObtenerCompraPorIdAsync(compra.CompraId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<CompraDto>.Fail($"Error al crear la compra: {ex.Message}");
            }
        }

        public async Task<Result<CompraDto>> RecibirCompraAsync(int compraId)
        {
            Compra? compra = await _context.Compras
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CompraId == compraId);

            if (compra is null)
                return Result<CompraDto>.Fail(
                    $"La compra con Id {compraId} no existe.");

            if (compra.EstadoCompraId != (int)EstadoCompraEnum.Pendiente)
                return Result<CompraDto>.Fail(
                    "Solo se pueden recibir compras en estado Pendiente.");

            List<int>? productosIds = compra.Detalles
                .Select(d => d.ProductoId)
                .ToList();

            List<Inventarios>? inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var detalle in compra.Detalles)
                {
                    Inventarios? inv = inventarios
                        .FirstOrDefault(i => i.ProductoId == detalle.ProductoId);

                    bool isNew = false;
                    if (inv is null)
                    {
                        inv = new Inventarios
                        {
                            ProductoId = detalle.ProductoId,
                            Cantidad = 0,
                            FechaIngreso = DateTime.UtcNow
                        };
                        _context.Inventario.Add(inv);
                        isNew = true;
                    }

                    inv.Cantidad += detalle.Cantidad;

                    HistorialMovimientoInventario historial = new()
                    {
                        Cantidad = detalle.Cantidad,
                        TipoMovimiento = TipoMovimientoEnum.ENTRADA.ToString(),
                        FechaMovimiento = DateTime.UtcNow
                    };

                    if (isNew)
                        historial.Inventario = inv;
                    else
                        historial.InventarioId = inv.InventarioId;

                    _context.HistorialMovimientoInventario.Add(historial);
                }

                compra.EstadoCompraId = (int)EstadoCompraEnum.Recibida;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await ObtenerCompraPorIdAsync(compraId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<CompraDto>.Fail($"Error al recibir la compra: {ex.Message}");
            }
        }


        public async Task<Result<CompraDto>> AnularCompraAsync(int compraId)
        {
            Compra? compra = await _context.Compras
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CompraId == compraId);

            if (compra is null)
                return Result<CompraDto>.Fail(
                    $"La compra con Id {compraId} no existe.");

            if (compra.EstadoCompraId == (int)EstadoCompraEnum.Anulada)
                return Result<CompraDto>.Fail(
                    "La compra ya se encuentra anulada.");

            if (compra.EstadoCompraId == (int)EstadoCompraEnum.Recibida)
            {
                List<int>? productosIds = compra.Detalles
                    .Select(d => d.ProductoId)
                    .ToList();

                List<Inventarios>? inventarios = await _context.Inventario
                    .Where(i => productosIds.Contains(i.ProductoId))
                    .ToListAsync();

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var detalle in compra.Detalles)
                    {
                        Inventarios? inv = inventarios
                            .FirstOrDefault(i => i.ProductoId == detalle.ProductoId);

                        if (inv is null || inv.Cantidad < detalle.Cantidad)
                        {
                            int disponible = inv?.Cantidad ?? 0;
                            await transaction.RollbackAsync();
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
                                TipoMovimiento = TipoMovimientoEnum.SALIDA.ToString(),
                                FechaMovimiento = DateTime.UtcNow
                            });
                    }

                    compra.EstadoCompraId = (int)EstadoCompraEnum.Anulada;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return await ObtenerCompraPorIdAsync(compraId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result<CompraDto>.Fail($"Error al anular la compra: {ex.Message}");
                }
            }

            compra.EstadoCompraId = (int)EstadoCompraEnum.Anulada;
            await _context.SaveChangesAsync();

            return await ObtenerCompraPorIdAsync(compraId);
        }

        public async Task<Result<PagoCompraDto>> RegistrarPagoAsync(CrearPagoCompraDto dto)
        {
            if (dto.Monto <= 0)
                return Result<PagoCompraDto>.Fail(
                    "El monto del pago debe ser mayor a cero.", isValidation: true);

            Compra? compra = await _context.Compras
                .Include(c => c.Pagos)
                .FirstOrDefaultAsync(c => c.CompraId == dto.CompraId);

            if (compra is null)
                return Result<PagoCompraDto>.Fail(
                    $"La compra con Id {dto.CompraId} no existe.");

            if (compra.EstadoCompraId == (int)EstadoCompraEnum.Anulada)
                return Result<PagoCompraDto>.Fail(
                    "No se puede registrar pago a una compra anulada.");

            decimal totalPagado = compra.Pagos.Sum(p => p.Monto);
            decimal pendientePago = compra.Total - totalPagado;

            if (dto.Monto > pendientePago)
                return Result<PagoCompraDto>.Fail(
                    $"El monto excede el saldo pendiente. " +
                    $"Pendiente: {pendientePago:C}, " +
                    $"Monto enviado: {dto.Monto:C}.");

            MetodosPago? metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == dto.MetodoPagoId);

            if (metodoPago is null)
                return Result<PagoCompraDto>.Fail(
                    $"El método de pago con Id {dto.MetodoPagoId} no existe.");

            PagosCompra pago = new()
            {
                CompraId = dto.CompraId,
                MetodoPagoId = dto.MetodoPagoId,
                Monto = dto.Monto,
                FechaPago = DateTime.UtcNow
            };

            _context.PagosCompra.Add(pago);
            await _context.SaveChangesAsync();

            return Result<PagoCompraDto>.Ok(new PagoCompraDto
            {
                PagoCompraId = pago.PagoCompraId,
                CompraId = pago.CompraId,
                MetodoPago = metodoPago.Nombre,
                Monto = pago.Monto,
                FechaPago = pago.FechaPago
            });
        }

        private static CompraDto MapearCompraResponse(Compra c)
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