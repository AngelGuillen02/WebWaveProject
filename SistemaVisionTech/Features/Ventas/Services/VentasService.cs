using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Ventas.Dtos;
using SistemaVisionTech.Features.Ventas.Enums;
using SistemaVisionTech.Features.Ventas.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using Entities = SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Ventas.Services
{
    public class VentasService : IVentasService
    {
        private readonly WebWaveDbContext _context;

        public VentasService(WebWaveDbContext context)
        {
            _context = context;
        }


        public async Task<Result<IEnumerable<VentasDto>>> ObtenerVentasAsync()
        {
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.EstadoVenta)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MetodoPago)
                .OrderByDescending(v => v.FechaVenta)
                .Select(v => MapearVentaResponse(v))
                .ToListAsync();

            return Result<IEnumerable<VentasDto>>.Ok(ventas);
        }

        public async Task<Result<VentasDto>> ObtenerVentaPorIdAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.EstadoVenta)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MetodoPago)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail(
                    $"La venta con Id {ventaId} no encontrada.");

            return Result<VentasDto>.Ok(MapearVentaResponse(venta));
        }

        // ─── CREAR VENTA ─────────────────────────────────────────────────

        public async Task<Result<VentasDto>> CrearVentaAsync(CrearVentaDto dto)
        {
            // Validaciones técnicas
            if (dto.Detalles is null || !dto.Detalles.Any())
                return Result<VentasDto>.Fail(
                    "La venta debe tener al menos un producto.", isValidation: true);

            if (dto.Detalles.Any(d => d.Cantidad <= 0))
                return Result<VentasDto>.Fail(
                    "La cantidad de cada producto debe ser mayor a cero.", isValidation: true);

            if (dto.Detalles.GroupBy(d => d.ProductoId).Any(g => g.Count() > 1))
                return Result<VentasDto>.Fail(
                    "No se puede repetir el mismo producto en los detalles. " +
                    "Ajuste la cantidad en un solo renglón.", isValidation: true);

            // Validar cliente
            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.ClienteId == dto.ClienteId);

            if (!clienteExiste)
                return Result<VentasDto>.Fail(
                    $"El cliente con Id {dto.ClienteId} no existe.");

            // Obtener productos e inventarios de una sola vez
            var productosIds = dto.Detalles.Select(d => d.ProductoId).ToList();

            var productos = await _context.Productos
                .Where(p => productosIds.Contains(p.ProductoId))
                .ToListAsync();

            // Validar que todos los productos existan
            var productosNoEncontrados = productosIds
                .Except(productos.Select(p => p.ProductoId))
                .ToList();

            if (productosNoEncontrados.Any())
                return Result<VentasDto>.Fail(
                    $"Los siguientes productos no existen: " +
                    $"{string.Join(", ", productosNoEncontrados)}.");

            var inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            // Validar stock por cada producto antes de crear nada
            foreach (var detalle in dto.Detalles)
            {
                var inv = inventarios
                    .FirstOrDefault(i => i.ProductoId == detalle.ProductoId);

                if (inv is null || inv.Cantidad < detalle.Cantidad)
                {
                    var disponible = inv?.Cantidad ?? 0;
                    var nombreProd = productos
                        .First(p => p.ProductoId == detalle.ProductoId).Nombre;

                    return Result<VentasDto>.Fail(
                        $"Stock insuficiente para '{nombreProd}'. " +
                        $"Disponible: {disponible}, Solicitado: {detalle.Cantidad}.");
                }
            }

            // Construir detalles y calcular total
            var detallesEntidad = new List<VentasDetalles>();

            foreach (var detalle in dto.Detalles)
            {
                var producto = productos.First(p => p.ProductoId == detalle.ProductoId);
                var totalLinea = producto.Precio * detalle.Cantidad;

                detallesEntidad.Add(new VentasDetalles
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    Precio = producto.Precio,
                    Total = totalLinea
                });
            }

            var totalVenta = detallesEntidad.Sum(d => d.Total);

            // Crear la venta en estado Pendiente
            var venta = new Entities.Ventas
            {
                ClienteId = dto.ClienteId,
                FechaVenta = DateTime.Now,
                Total = totalVenta,
                EstadoVentaId = (int)EstadoVentaEnum.Pendiente,
                Detalles = detallesEntidad
            };

            _context.Ventas.Add(venta);

            // Descontar stock e insertar movimientos de inventario
            foreach (var detalle in dto.Detalles)
            {
                var inv = inventarios.First(i => i.ProductoId == detalle.ProductoId);
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

            await _context.SaveChangesAsync();

            return await ObtenerVentaPorIdAsync(venta.VentaId);
        }

        // ─── CONFIRMAR VENTA ─────────────────────────────────────────────

        public async Task<Result<VentasDto>> ConfirmarVentaAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail(
                    $"La venta con Id {ventaId} no existe.");

            if (venta.EstadoVentaId != (int)EstadoVentaEnum.Pendiente)
                return Result<VentasDto>.Fail(
                    "Solo se pueden confirmar ventas en estado Pendiente.");

            venta.EstadoVentaId = (int)EstadoVentaEnum.Confirmada;
            await _context.SaveChangesAsync();

            return await ObtenerVentaPorIdAsync(ventaId);
        }

        // ─── ANULAR VENTA ────────────────────────────────────────────────

        public async Task<Result<VentasDto>> AnularVentaAsync(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail(
                    $"La venta con Id {ventaId} no existe.");

            if (venta.EstadoVentaId == (int)EstadoVentaEnum.Anulada)
                return Result<VentasDto>.Fail(
                    "La venta ya se encuentra anulada.");

            // Restaurar stock por cada producto del detalle
            var productosIds = venta.Detalles
                .Select(d => d.ProductoId)
                .ToList();

            var inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            foreach (var detalle in venta.Detalles)
            {
                var inv = inventarios
                    .First(i => i.ProductoId == detalle.ProductoId);

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

            venta.EstadoVentaId = (int)EstadoVentaEnum.Anulada;
            await _context.SaveChangesAsync();

            return await ObtenerVentaPorIdAsync(ventaId);
        }

        // ─── REGISTRAR PAGO ──────────────────────────────────────────────

        public async Task<Result<PagoVentaDto>> RegistrarPagoAsync(CrearPagoVentaDto dto)
        {
            if (dto.Monto <= 0)
                return Result<PagoVentaDto>.Fail(
                    "El monto del pago debe ser mayor a cero.", isValidation: true);

            var venta = await _context.Ventas
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.VentaId == dto.VentaId);

            if (venta is null)
                return Result<PagoVentaDto>.Fail(
                    $"La venta con Id {dto.VentaId} no existe.");

            if (venta.EstadoVentaId != (int)EstadoVentaEnum.Confirmada)
                return Result<PagoVentaDto>.Fail(
                    "Solo se puede registrar pago a ventas Confirmadas.");

            // Validar que no se exceda el total
            var totalPagado = venta.Pagos.Sum(p => p.Monto);
            var pendientePago = venta.Total - totalPagado;

            if (dto.Monto > pendientePago)
                return Result<PagoVentaDto>.Fail(
                    $"El monto excede el saldo pendiente. " +
                    $"Pendiente: {pendientePago:C}, Monto enviado: {dto.Monto:C}.");

            // Validar método de pago
            var metodoPagoExiste = await _context.MetodosPago
                .AnyAsync(m => m.MetodoPagoId == dto.MetodoPagoId);

            if (!metodoPagoExiste)
                return Result<PagoVentaDto>.Fail(
                    $"El método de pago con Id {dto.MetodoPagoId} no existe.");

            var pago = new PagosVenta
            {
                VentaId = dto.VentaId,
                MetodoPagoId = dto.MetodoPagoId,
                Monto = dto.Monto,
                FechaPago = DateTime.Now
            };

            _context.PagosVenta.Add(pago);
            await _context.SaveChangesAsync();

            // Cargar método de pago para respuesta
            var nombreMetodo = await _context.MetodosPago
                .Where(m => m.MetodoPagoId == dto.MetodoPagoId)
                .Select(m => m.Nombre)
                .FirstAsync();

            return Result<PagoVentaDto>.Ok(new PagoVentaDto
            {
                PagoVentaId = pago.PagoVentaId,
                VentaId = pago.VentaId,
                MetodoPago = nombreMetodo,
                Monto = pago.Monto,
                FechaPago = pago.FechaPago
            });
        }

        // ─── MAPPER PRIVADO ──────────────────────────────────────────────

        private static VentasDto MapearVentaResponse(Entities.Ventas v)
        {
            return new VentasDto
            {
                VentaId = v.VentaId,
                ClienteId = v.ClienteId,
                Cliente = v.Cliente.Nombre,
                FechaVenta = v.FechaVenta,
                Total = v.Total,
                EstadoVenta = v.EstadoVenta.Nombre,
                Detalles = v.Detalles.Select(d => new VentaDetalleDto
                {
                    VentaDetalleId = d.VentaDetalleId,
                    ProductoId = d.ProductoId,
                    Producto = d.Producto.Nombre,
                    Cantidad = d.Cantidad,
                    Precio = d.Precio,
                    Total = d.Total
                }).ToList(),
                Pagos = v.Pagos.Select(p => new PagoVentaDto
                {
                    PagoVentaId = p.PagoVentaId,
                    VentaId = p.VentaId,
                    MetodoPago = p.MetodoPago.Nombre,
                    Monto = p.Monto,
                    FechaPago = p.FechaPago
                }).ToList()
            };
        }
    }
}