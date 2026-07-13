using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Inventario.Enums;
using SistemaVisionTech.Features.Ventas.Dtos;
using SistemaVisionTech.Features.Ventas.Enums;
using SistemaVisionTech.Features.Ventas.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

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
            List<VentasDto> ventas = await _context.Ventas
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
            Venta? venta = await _context.Ventas
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


        public async Task<Result<VentasDto>> CrearVentaAsync(CrearVentaDto dto)
        {
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

            bool clienteExiste = await _context.Clientes
                .AnyAsync(c => c.ClienteId == dto.ClienteId);

            if (!clienteExiste)
                return Result<VentasDto>.Fail(
                    $"El cliente con Id {dto.ClienteId} no existe.");

            List<int>? productosIds = dto.Detalles.Select(d => d.ProductoId).ToList();

            List<Producto>? productos = await _context.Productos
                .Where(p => productosIds.Contains(p.ProductoId))
                .ToListAsync();

            List<int>? productosNoEncontrados = productosIds
                .Except(productos.Select(p => p.ProductoId))
                .ToList();

            if (productosNoEncontrados.Any())
                return Result<VentasDto>.Fail(
                    $"Los siguientes productos no existen: " +
                    $"{string.Join(", ", productosNoEncontrados)}.");

            List<Inventarios>? inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            foreach (var detalle in dto.Detalles)
            {
                Inventarios? inv = inventarios
                    .FirstOrDefault(i => i.ProductoId == detalle.ProductoId);

                if (inv is null || inv.Cantidad < detalle.Cantidad)
                {
                    int disponible = inv?.Cantidad ?? 0;
                    string nombreProd = productos
                        .First(p => p.ProductoId == detalle.ProductoId).Nombre;

                    return Result<VentasDto>.Fail(
                        $"Stock insuficiente para '{nombreProd}'. " +
                        $"Disponible: {disponible}, Solicitado: {detalle.Cantidad}.");
                }
            }

            List<VentasDetalles> detallesEntidad = [];

            foreach (var detalle in dto.Detalles)
            {
                Producto producto = productos.First(p => p.ProductoId == detalle.ProductoId);
                decimal totalLinea = producto.Precio * detalle.Cantidad;

                detallesEntidad.Add(new VentasDetalles
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    Precio = producto.Precio,
                    Total = totalLinea
                });
            }

            decimal totalVenta = detallesEntidad.Sum(d => d.Total);

            Venta venta = new()
            {
                ClienteId = dto.ClienteId,
                FechaVenta = DateTime.UtcNow,
                Total = totalVenta,
                EstadoVentaId = (int)EstadoVentaEnum.Pendiente,
                Detalles = detallesEntidad
            };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Ventas.Add(venta);

                foreach (var detalle in dto.Detalles)
                {
                    Inventarios? inv = inventarios.First(i => i.ProductoId == detalle.ProductoId);
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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await ObtenerVentaPorIdAsync(venta.VentaId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<VentasDto>.Fail($"Error al crear la venta: {ex.Message}");
            }
        }


        public async Task<Result<VentasDto>> ConfirmarVentaAsync(int ventaId)
        {
            Venta? venta = await _context.Ventas
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

        public async Task<Result<VentasDto>> AnularVentaAsync(int ventaId)
        {
            Venta? venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail(
                    $"La venta con Id {ventaId} no existe.");

            if (venta.EstadoVentaId == (int)EstadoVentaEnum.Anulada)
                return Result<VentasDto>.Fail(
                    "La venta ya se encuentra anulada.");

            List<int>? productosIds = venta.Detalles
                .Select(d => d.ProductoId)
                .ToList();

            List<Inventarios>? inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var detalle in venta.Detalles)
                {
                    Inventarios? inv = inventarios
                        .First(i => i.ProductoId == detalle.ProductoId);

                    inv.Cantidad += detalle.Cantidad;

                    _context.HistorialMovimientoInventario.Add(
                        new HistorialMovimientoInventario
                        {
                            InventarioId = inv.InventarioId,
                            Cantidad = detalle.Cantidad,
                            TipoMovimiento = TipoMovimientoEnum.ENTRADA.ToString(),
                            FechaMovimiento = DateTime.UtcNow
                        });
                }

                venta.EstadoVentaId = (int)EstadoVentaEnum.Anulada;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await ObtenerVentaPorIdAsync(ventaId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<VentasDto>.Fail($"Error al anular la venta: {ex.Message}");
            }
        }

        public async Task<Result<PagoVentaDto>> RegistrarPagoAsync(CrearPagoVentaDto dto)
        {
            if (dto.Monto <= 0)
                return Result<PagoVentaDto>.Fail(
                    "El monto del pago debe ser mayor a cero.", isValidation: true);

            Venta? venta = await _context.Ventas
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.VentaId == dto.VentaId);

            if (venta is null)
                return Result<PagoVentaDto>.Fail(
                    $"La venta con Id {dto.VentaId} no existe.");

            if (venta.EstadoVentaId != (int)EstadoVentaEnum.Confirmada)
                return Result<PagoVentaDto>.Fail(
                    "Solo se puede registrar pago a ventas Confirmadas.");

            decimal totalPagado = venta.Pagos.Sum(p => p.Monto);
            decimal pendientePago = venta.Total - totalPagado;

            if (dto.Monto > pendientePago)
                return Result<PagoVentaDto>.Fail(
                    $"El monto excede el saldo pendiente. " +
                    $"Pendiente: {pendientePago:C}, Monto enviado: {dto.Monto:C}.");

            MetodosPago? metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == dto.MetodoPagoId);

            if (metodoPago is null)
                return Result<PagoVentaDto>.Fail(
                    $"El método de pago con Id {dto.MetodoPagoId} no existe.");

            PagosVenta pago = new()
            {
                VentaId = dto.VentaId,
                MetodoPagoId = dto.MetodoPagoId,
                Monto = dto.Monto,
                FechaPago = DateTime.UtcNow
            };

            _context.PagosVenta.Add(pago);
            await _context.SaveChangesAsync();



            return Result<PagoVentaDto>.Ok(new PagoVentaDto
            {
                PagoVentaId = pago.PagoVentaId,
                VentaId = pago.VentaId,
                MetodoPago = metodoPago.Nombre,
                Monto = pago.Monto,
                FechaPago = pago.FechaPago
            });
        }


        private static VentasDto MapearVentaResponse(Venta v)
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