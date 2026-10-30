using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Inventario.Enums;
using SistemaVisionTech.Features.SAR.Interfaces;
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
        private readonly ISARService _sarService;

        public VentasService(WebWaveDbContext context, ISARService sarService)
        {
            _context = context;
            _sarService = sarService;
        }

        public async Task<Result<IEnumerable<VentasDto>>> ObtenerVentasAsync()
        {
            List<Venta> ventas = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.EstadoVenta)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MetodoPago)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            List<int> ventaIds = ventas.Select(v => v.VentaId).ToList();

            List<FacturasEmitidas> facturas = await _context.FacturasEmitidas
                .AsNoTracking()
                .Where(f => ventaIds.Contains(f.VentaId) && f.Activo)
                .ToListAsync();

            Dictionary<int, FacturasEmitidas> facturasDict = facturas
                .GroupBy(f => f.VentaId)
                .ToDictionary(g => g.Key, g => g.First());

            IEnumerable<VentasDto> dtos = ventas.Select(v =>
                MapearVentaResponse(v, facturasDict.GetValueOrDefault(v.VentaId)));

            return Result<IEnumerable<VentasDto>>.Ok(dtos);
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
                return Result<VentasDto>.Fail($"La venta con Id {ventaId} no fue encontrada.");

            FacturasEmitidas? factura = await _context.FacturasEmitidas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.VentaId == ventaId && f.Activo);

            return Result<VentasDto>.Ok(MapearVentaResponse(venta, factura));
        }

        public async Task<Result<VentasDto>> CrearVentaAsync(CrearVentaDto dto)
        {
            if (dto.Detalles is null || !dto.Detalles.Any())
                return Result<VentasDto>.Fail("La venta debe tener al menos un producto.", isValidation: true);

            if (dto.Detalles.Any(d => d.Cantidad <= 0))
                return Result<VentasDto>.Fail("La cantidad de cada producto debe ser mayor a cero.", isValidation: true);

            if (dto.Detalles.GroupBy(d => d.ProductoId).Any(g => g.Count() > 1))
                return Result<VentasDto>.Fail("No se puede repetir el mismo producto en los renglones de detalle.", isValidation: true);

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId);

            if (cliente is null)
                return Result<VentasDto>.Fail($"El cliente con Id {dto.ClienteId} no existe.");

            List<int> productosIds = dto.Detalles.Select(d => d.ProductoId).ToList();

            List<Producto> productos = await _context.Productos
                .Where(p => productosIds.Contains(p.ProductoId))
                .ToListAsync();

            List<int> productosNoEncontrados = productosIds
                .Except(productos.Select(p => p.ProductoId))
                .ToList();

            if (productosNoEncontrados.Any())
                return Result<VentasDto>.Fail($"Los siguientes productos no existen: {string.Join(", ", productosNoEncontrados)}.");

            List<Inventarios> inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            foreach (var detalle in dto.Detalles)
            {
                Inventarios? inv = inventarios.FirstOrDefault(i => i.ProductoId == detalle.ProductoId);
                int disponible = inv?.Cantidad ?? 0;

                if (inv is null || disponible < detalle.Cantidad)
                {
                    string nombreProd = productos.First(p => p.ProductoId == detalle.ProductoId).Nombre;
                    return Result<VentasDto>.Fail($"Stock insuficiente para '{nombreProd}'. Disponible: {disponible}, Solicitado: {detalle.Cantidad}.");
                }
            }

            List<VentasDetalles> detallesEntidad = [];
            foreach (var detalle in dto.Detalles)
            {
                Producto producto = productos.First(p => p.ProductoId == detalle.ProductoId);
                decimal precioUnitario = detalle.PrecioUnitario > 0 ? detalle.PrecioUnitario : producto.Precio;
                decimal subtotal = precioUnitario * detalle.Cantidad;
                decimal totalLinea = Math.Max(0, subtotal - detalle.Descuento);

                detallesEntidad.Add(new VentasDetalles
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    Precio = precioUnitario,
                    Total = totalLinea
                });
            }

            decimal totalVenta = detallesEntidad.Sum(d => d.Total);

            Venta venta = new()
            {
                ClienteId = dto.ClienteId,
                FechaVenta = DateTime.UtcNow,
                Total = totalVenta,
                EstadoVentaId = dto.ConcretarVenta ? (int)EstadoVentaEnum.Confirmada : (int)EstadoVentaEnum.Pendiente,
                Rtn = cliente.RTN ?? string.Empty,
                Detalles = detallesEntidad
            };

            if (dto.ConcretarVenta)
            {
                int metodoId = dto.MetodoPagoId ?? 1;
                venta.Pagos.Add(new PagosVenta
                {
                    MetodoPagoId = metodoId,
                    Monto = totalVenta,
                    FechaPago = DateTime.UtcNow
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Ventas.Add(venta);

                foreach (var detalle in dto.Detalles)
                {
                    Inventarios inv = inventarios.First(i => i.ProductoId == detalle.ProductoId);
                    inv.Cantidad -= detalle.Cantidad;

                    _context.HistorialMovimientoInventario.Add(new HistorialMovimientoInventario
                    {
                        InventarioId = inv.InventarioId,
                        Cantidad = detalle.Cantidad,
                        TipoMovimiento = TipoMovimientoEnum.SALIDA.ToString(),
                        FechaMovimiento = DateTime.UtcNow,
                        OrigenTipo = "Venta"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<VentasDto>.Fail($"Error al registrar la venta: {ex.Message}");
            }

            // Si se solicitó concretar y facturar con CAI del SAR
            if (dto.ConcretarVenta && dto.EmitirFactura)
            {
                try
                {
                    await _sarService.EmitirFacturaAsync(venta.VentaId, dto.SucursalId);
                }
                catch (Exception)
                {
                    // La venta quedó registrada, pero se alerta que el CAI debe revisarse
                }
            }

            return await ObtenerVentaPorIdAsync(venta.VentaId);
        }

        public async Task<Result<VentasDto>> ConfirmarVentaAsync(int ventaId)
        {
            Venta? venta = await _context.Ventas
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail($"La venta con Id {ventaId} no existe.");

            if (venta.EstadoVentaId != (int)EstadoVentaEnum.Pendiente)
                return Result<VentasDto>.Fail("Solo se pueden confirmar ventas en estado Pendiente.");

            venta.EstadoVentaId = (int)EstadoVentaEnum.Confirmada;
            await _context.SaveChangesAsync();

            // Emitir factura automáticamente al confirmar si no la tiene
            await _sarService.EmitirFacturaAsync(ventaId, 1);

            return await ObtenerVentaPorIdAsync(ventaId);
        }

        public async Task<Result<VentasDto>> AnularVentaAsync(int ventaId)
        {
            Venta? venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail($"La venta con Id {ventaId} no existe.");

            if (venta.EstadoVentaId == (int)EstadoVentaEnum.Anulada)
                return Result<VentasDto>.Fail("La venta ya se encuentra anulada.");

            List<int> productosIds = venta.Detalles.Select(d => d.ProductoId).ToList();

            List<Inventarios> inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var detalle in venta.Detalles)
                {
                    Inventarios? inv = inventarios.FirstOrDefault(i => i.ProductoId == detalle.ProductoId);
                    if (inv != null)
                    {
                        inv.Cantidad += detalle.Cantidad;

                        _context.HistorialMovimientoInventario.Add(new HistorialMovimientoInventario
                        {
                            InventarioId = inv.InventarioId,
                            Cantidad = detalle.Cantidad,
                            TipoMovimiento = TipoMovimientoEnum.ENTRADA.ToString(),
                            FechaMovimiento = DateTime.UtcNow,
                            OrigenTipo = "AnulacionVenta"
                        });
                    }
                }

                venta.EstadoVentaId = (int)EstadoVentaEnum.Anulada;

                // Anular factura si fue emitida
                FacturasEmitidas? factura = await _context.FacturasEmitidas
                    .FirstOrDefaultAsync(f => f.VentaId == ventaId);
                if (factura != null)
                {
                    factura.Activo = false;
                }

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
                return Result<PagoVentaDto>.Fail("El monto del pago debe ser mayor a cero.", isValidation: true);

            Venta? venta = await _context.Ventas
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.VentaId == dto.VentaId);

            if (venta is null)
                return Result<PagoVentaDto>.Fail($"La venta con Id {dto.VentaId} no existe.");

            decimal totalPagado = venta.Pagos.Sum(p => p.Monto);
            decimal pendientePago = venta.Total - totalPagado;

            if (dto.Monto > pendientePago)
                return Result<PagoVentaDto>.Fail($"El monto excede el saldo pendiente. Pendiente: {pendientePago:C}, Monto enviado: {dto.Monto:C}.");

            MetodosPago? metodoPago = await _context.MetodosPago
                .FirstOrDefaultAsync(m => m.MetodoPagoId == dto.MetodoPagoId);

            if (metodoPago is null)
                return Result<PagoVentaDto>.Fail($"El método de pago con Id {dto.MetodoPagoId} no existe.");

            PagosVenta pago = new()
            {
                VentaId = dto.VentaId,
                MetodoPagoId = dto.MetodoPagoId,
                Monto = dto.Monto,
                FechaPago = DateTime.UtcNow
            };

            _context.PagosVenta.Add(pago);

            if (totalPagado + dto.Monto >= venta.Total)
            {
                venta.EstadoVentaId = (int)EstadoVentaEnum.Confirmada;
                // Emitir factura automáticamente
                await _sarService.EmitirFacturaAsync(venta.VentaId, 1);
            }

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

        private static VentasDto MapearVentaResponse(Venta v, FacturasEmitidas? factura = null)
        {
            return new VentasDto
            {
                VentaId = v.VentaId,
                ClienteId = v.ClienteId,
                Cliente = v.Cliente?.Nombre ?? "Cliente general",
                FechaVenta = v.FechaVenta,
                Total = v.Total,
                EstadoVenta = v.EstadoVenta?.Nombre ?? (v.EstadoVentaId == 2 ? "Confirmada" : "Pendiente"),
                NumeroFactura = factura?.NumeroFactura,
                CAI = factura?.CAI,
                ISV15 = factura?.ISV15,
                ISV18 = factura?.ISV18,
                MontoExento = factura?.MontoExento,
                MontoGravado15 = factura?.MontoGravado15,
                FechaFactura = factura?.FechaEmision,
                Detalles = v.Detalles.Select(d => new VentaDetalleDto
                {
                    VentaDetalleId = d.VentaDetalleId,
                    ProductoId = d.ProductoId,
                    Producto = d.Producto?.Nombre ?? $"Producto #{d.ProductoId}",
                    Cantidad = d.Cantidad,
                    Precio = d.Precio,
                    Total = d.Total
                }).ToList(),
                Pagos = v.Pagos.Select(p => new PagoVentaDto
                {
                    PagoVentaId = p.PagoVentaId,
                    VentaId = p.VentaId,
                    MetodoPago = p.MetodoPago?.Nombre ?? "Efectivo",
                    Monto = p.Monto,
                    FechaPago = p.FechaPago
                }).ToList()
            };
        }
    }
}
