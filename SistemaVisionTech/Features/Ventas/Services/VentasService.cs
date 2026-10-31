using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Inventario.Enums;
using SistemaVisionTech.Features.SAR.Interfaces;
using SistemaVisionTech.Features.Ventas.Dtos;
using SistemaVisionTech.Features.Ventas.Enums;
using SistemaVisionTech.Features.Ventas.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using System.Text;

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
                return Result<VentasDto>.Fail($"El documento con Id {ventaId} no fue encontrado.");

            FacturasEmitidas? factura = await _context.FacturasEmitidas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.VentaId == ventaId && f.Activo);

            return Result<VentasDto>.Ok(MapearVentaResponse(venta, factura));
        }

        public async Task<Result<VentasDto>> CrearVentaAsync(CrearVentaDto dto)
        {
            if (dto.Detalles is null || !dto.Detalles.Any())
                return Result<VentasDto>.Fail("El documento debe tener al menos un producto.", isValidation: true);

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

            // ==========================================
            // FLUJO 1: COTIZACIÓN (PROFORMA / ESTIMADO)
            // ==========================================
            if (dto.EsCotizacion)
            {
                Venta cotizacion = new()
                {
                    ClienteId = dto.ClienteId,
                    FechaVenta = DateTime.UtcNow,
                    Total = totalVenta,
                    EstadoVentaId = (int)EstadoVentaEnum.Cotizacion,
                    EsCotizacion = true,
                    Rtn = cliente.RTN ?? string.Empty,
                    Detalles = detallesEntidad
                };

                using var transCot = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Ventas.Add(cotizacion);
                    await _context.SaveChangesAsync();

                    // Asignar identificador correlativo de cotización COT-000001
                    cotizacion.NumeroCotizacion = $"COT-{cotizacion.VentaId:D6}";

                    // Registro inicial en bitácora de auditoría
                    _context.CotizacionesHistorial.Add(new CotizacionesHistorial
                    {
                        VentaId = cotizacion.VentaId,
                        NumeroCotizacion = cotizacion.NumeroCotizacion,
                        Usuario = !string.IsNullOrWhiteSpace(dto.UsuarioNombre) ? dto.UsuarioNombre : "Usuario",
                        FechaModificacion = DateTime.UtcNow,
                        Accion = "CREACIÓN",
                        DetalleCambios = $"Creación inicial de cotización con {detallesEntidad.Count} ítem(s). Total: L. {totalVenta:N2}",
                        TotalAnterior = null,
                        TotalNuevo = totalVenta,
                        Motivo = "Creación de cotización",
                        Activo = true
                    });

                    await _context.SaveChangesAsync();
                    await transCot.CommitAsync();

                    return await ObtenerVentaPorIdAsync(cotizacion.VentaId);
                }
                catch (Exception ex)
                {
                    await transCot.RollbackAsync();
                    return Result<VentasDto>.Fail($"Error al generar la cotización: {ex.Message}");
                }
            }

            // ==========================================
            // FLUJO 2: FACTURA FISCAL SAR
            // ==========================================
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

            Venta venta = new()
            {
                ClienteId = dto.ClienteId,
                FechaVenta = DateTime.UtcNow,
                Total = totalVenta,
                EstadoVentaId = dto.ConcretarVenta ? (int)EstadoVentaEnum.Confirmada : (int)EstadoVentaEnum.Pendiente,
                EsCotizacion = false,
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
                return Result<VentasDto>.Fail($"Error al registrar la factura fiscal: {ex.Message}");
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

        public async Task<Result<VentasDto>> ActualizarCotizacionAsync(int ventaId, ActualizarCotizacionDto dto, string usuarioNombre)
        {
            Venta? venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail($"El documento con Id {ventaId} no existe.");

            // REGLA ESTRICTA SAR: Las facturas fiscales emitidas NO pueden ser editadas
            if (!venta.EsCotizacion || venta.EstadoVentaId != (int)EstadoVentaEnum.Cotizacion)
            {
                return Result<VentasDto>.Fail("Las facturas fiscales emitidas no pueden ser editadas por normativa SAR. Solo las cotizaciones admiten modificaciones.", isValidation: true);
            }

            if (venta.EstadoVentaId == (int)EstadoVentaEnum.Anulada)
            {
                return Result<VentasDto>.Fail("No es posible modificar una cotización que ha sido anulada.", isValidation: true);
            }

            if (dto.Detalles is null || !dto.Detalles.Any())
                return Result<VentasDto>.Fail("La cotización debe tener al menos un producto.", isValidation: true);

            if (dto.Detalles.Any(d => d.Cantidad <= 0))
                return Result<VentasDto>.Fail("La cantidad de cada producto debe ser mayor a cero.", isValidation: true);

            if (dto.Detalles.GroupBy(d => d.ProductoId).Any(g => g.Count() > 1))
                return Result<VentasDto>.Fail("No se puede repetir el mismo producto en los renglones de la cotización.", isValidation: true);

            Cliente? nuevoCliente = await _context.Clientes.FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId);
            if (nuevoCliente is null)
                return Result<VentasDto>.Fail($"El cliente con Id {dto.ClienteId} no existe.");

            List<int> productosIds = dto.Detalles.Select(d => d.ProductoId).ToList();
            List<Producto> productos = await _context.Productos
                .Where(p => productosIds.Contains(p.ProductoId))
                .ToListAsync();

            List<int> noEncontrados = productosIds.Except(productos.Select(p => p.ProductoId)).ToList();
            if (noEncontrados.Any())
                return Result<VentasDto>.Fail($"Los siguientes productos no existen: {string.Join(", ", noEncontrados)}.");

            decimal totalAnterior = venta.Total;
            string clienteAnterior = venta.Cliente?.Nombre ?? $"Cliente #{venta.ClienteId}";

            // Construir desglose detallado de modificaciones para auditoría
            StringBuilder cambiosSb = new();
            if (venta.ClienteId != dto.ClienteId)
            {
                cambiosSb.AppendLine($"Cliente cambiado: '{clienteAnterior}' → '{nuevoCliente.Nombre}'.");
            }

            // Comparar detalles anteriores vs nuevos
            Dictionary<int, VentasDetalles> viejosDetallesDict = venta.Detalles.ToDictionary(d => d.ProductoId, d => d);
            Dictionary<int, CrearVentaDetalleDto> nuevosDetallesDict = dto.Detalles.ToDictionary(d => d.ProductoId, d => d);

            foreach (var kvp in nuevosDetallesDict)
            {
                Producto prod = productos.First(p => p.ProductoId == kvp.Key);
                if (!viejosDetallesDict.TryGetValue(kvp.Key, out VentasDetalles? viejo))
                {
                    cambiosSb.AppendLine($"Producto agregado: '{prod.Nombre}' (Cant: {kvp.Value.Cantidad}, Precio: L. {kvp.Value.PrecioUnitario:N2}).");
                }
                else
                {
                    if (viejo.Cantidad != kvp.Value.Cantidad || viejo.Precio != kvp.Value.PrecioUnitario)
                    {
                        cambiosSb.AppendLine($"Producto '{prod.Nombre}' modificado: Cantidad {viejo.Cantidad} → {kvp.Value.Cantidad}, Precio L. {viejo.Precio:N2} → L. {kvp.Value.PrecioUnitario:N2}.");
                    }
                }
            }

            foreach (var kvp in viejosDetallesDict)
            {
                if (!nuevosDetallesDict.ContainsKey(kvp.Key))
                {
                    string nom = kvp.Value.Producto?.Nombre ?? $"Producto #{kvp.Key}";
                    cambiosSb.AppendLine($"Producto eliminado: '{nom}' (era Cant: {kvp.Value.Cantidad}).");
                }
            }

            // Eliminar detalles anteriores
            _context.VentasDetalles.RemoveRange(venta.Detalles);

            // Crear y agregar nuevos detalles
            List<VentasDetalles> nuevosDetalles = [];
            foreach (var det in dto.Detalles)
            {
                Producto prod = productos.First(p => p.ProductoId == det.ProductoId);
                decimal pu = det.PrecioUnitario > 0 ? det.PrecioUnitario : prod.Precio;
                decimal subtotal = pu * det.Cantidad;
                decimal totLinea = Math.Max(0, subtotal - det.Descuento);

                nuevosDetalles.Add(new VentasDetalles
                {
                    VentaId = ventaId,
                    ProductoId = det.ProductoId,
                    Cantidad = det.Cantidad,
                    Precio = pu,
                    Total = totLinea
                });
            }

            decimal nuevoTotal = nuevosDetalles.Sum(d => d.Total);

            if (totalAnterior != nuevoTotal)
            {
                cambiosSb.AppendLine($"Total ajustado: L. {totalAnterior:N2} → L. {nuevoTotal:N2}.");
            }

            venta.ClienteId = dto.ClienteId;
            venta.Total = nuevoTotal;
            venta.Rtn = nuevoCliente.RTN ?? string.Empty;
            venta.Detalles = nuevosDetalles;

            string resumenCambios = cambiosSb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(resumenCambios))
            {
                resumenCambios = "Se actualizaron datos generales de la cotización sin cambios de monto.";
            }

            // Registrar en tabla inmutable de auditoría
            _context.CotizacionesHistorial.Add(new CotizacionesHistorial
            {
                VentaId = ventaId,
                NumeroCotizacion = venta.NumeroCotizacion,
                Usuario = !string.IsNullOrWhiteSpace(usuarioNombre) ? usuarioNombre : "Usuario",
                FechaModificacion = DateTime.UtcNow,
                Accion = "MODIFICACIÓN",
                DetalleCambios = resumenCambios,
                TotalAnterior = totalAnterior,
                TotalNuevo = nuevoTotal,
                Motivo = !string.IsNullOrWhiteSpace(dto.Motivo) ? dto.Motivo : "Ajuste de ítems o precios a solicitud del cliente",
                Activo = true
            });

            await _context.SaveChangesAsync();

            return await ObtenerVentaPorIdAsync(ventaId);
        }

        public async Task<Result<IEnumerable<CotizacionHistorialDto>>> ObtenerHistorialCotizacionAsync(int ventaId)
        {
            List<CotizacionHistorialDto> historial = await _context.CotizacionesHistorial
                .AsNoTracking()
                .Where(h => h.VentaId == ventaId && h.Activo)
                .OrderByDescending(h => h.FechaModificacion)
                .Select(h => new CotizacionHistorialDto
                {
                    Id = h.Id,
                    VentaId = h.VentaId,
                    NumeroCotizacion = h.NumeroCotizacion,
                    Usuario = h.Usuario,
                    FechaModificacion = h.FechaModificacion,
                    Accion = h.Accion,
                    DetalleCambios = h.DetalleCambios,
                    TotalAnterior = h.TotalAnterior,
                    TotalNuevo = h.TotalNuevo,
                    Motivo = h.Motivo
                })
                .ToListAsync();

            return Result<IEnumerable<CotizacionHistorialDto>>.Ok(historial);
        }

        public async Task<Result<VentasDto>> FacturarCotizacionAsync(int ventaId, int sucursalId, int metodoPagoId, string usuarioNombre)
        {
            Venta? venta = await _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<VentasDto>.Fail($"La cotización con Id {ventaId} no existe.");

            if (!venta.EsCotizacion || venta.EstadoVentaId != (int)EstadoVentaEnum.Cotizacion)
                return Result<VentasDto>.Fail("El documento no es una cotización pendiente o ya fue facturado.");

            List<int> productosIds = venta.Detalles.Select(d => d.ProductoId).ToList();
            List<Inventarios> inventarios = await _context.Inventario
                .Where(i => productosIds.Contains(i.ProductoId))
                .ToListAsync();

            // Validar stock antes de convertir la cotización a factura
            foreach (var detalle in venta.Detalles)
            {
                Inventarios? inv = inventarios.FirstOrDefault(i => i.ProductoId == detalle.ProductoId);
                int disponible = inv?.Cantidad ?? 0;

                if (inv is null || disponible < detalle.Cantidad)
                {
                    string nom = detalle.Producto?.Nombre ?? $"Producto #{detalle.ProductoId}";
                    return Result<VentasDto>.Fail($"Stock insuficiente para facturar '{nom}'. Disponible: {disponible}, Requerido: {detalle.Cantidad}.");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Rebajar inventario y registrar Kardex
                foreach (var detalle in venta.Detalles)
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

                // Registrar pago
                venta.Pagos.Add(new PagosVenta
                {
                    MetodoPagoId = metodoPagoId > 0 ? metodoPagoId : 1,
                    Monto = venta.Total,
                    FechaPago = DateTime.UtcNow
                });

                // Cambiar a venta formal confirmada
                venta.EsCotizacion = false;
                venta.EstadoVentaId = (int)EstadoVentaEnum.Confirmada;

                // Bitácora de auditoría
                _context.CotizacionesHistorial.Add(new CotizacionesHistorial
                {
                    VentaId = ventaId,
                    NumeroCotizacion = venta.NumeroCotizacion,
                    Usuario = !string.IsNullOrWhiteSpace(usuarioNombre) ? usuarioNombre : "Usuario",
                    FechaModificacion = DateTime.UtcNow,
                    Accion = "FACTURADA",
                    DetalleCambios = $"Cotización {venta.NumeroCotizacion} aprobada y convertida en Factura Fiscal SAR. Total: L. {venta.Total:N2}.",
                    TotalAnterior = venta.Total,
                    TotalNuevo = venta.Total,
                    Motivo = "Aprobación y emisión de factura fiscal",
                    Activo = true
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string detalleError = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                return Result<VentasDto>.Fail($"Error al facturar la cotización: {detalleError}");
            }

            try
            {
                // Emitir factura SAR oficial con CAI
                await _sarService.EmitirFacturaAsync(ventaId, sucursalId);
            }
            catch (Exception)
            {
                // Factura registrada en base
            }

            return await ObtenerVentaPorIdAsync(ventaId);
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
                return Result<VentasDto>.Fail($"El documento con Id {ventaId} no existe.");

            if (venta.EstadoVentaId == (int)EstadoVentaEnum.Anulada)
                return Result<VentasDto>.Fail("El documento ya se encuentra anulado.");

            // Si es cotización, simplemente se marca como anulada (no afectó inventario)
            if (venta.EsCotizacion)
            {
                venta.EstadoVentaId = (int)EstadoVentaEnum.Anulada;
                _context.CotizacionesHistorial.Add(new CotizacionesHistorial
                {
                    VentaId = ventaId,
                    NumeroCotizacion = venta.NumeroCotizacion,
                    Usuario = "Usuario",
                    FechaModificacion = DateTime.UtcNow,
                    Accion = "ANULADA",
                    DetalleCambios = "Cotización anulada/cancelada.",
                    TotalAnterior = venta.Total,
                    TotalNuevo = 0,
                    Motivo = "Cancelación de cotización",
                    Activo = true
                });

                await _context.SaveChangesAsync();
                return await ObtenerVentaPorIdAsync(ventaId);
            }

            // Para facturas formales, reponer inventario
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
            string estadoTexto = v.EsCotizacion
                ? (v.EstadoVentaId == 3 ? "Anulada" : "Cotización")
                : (v.EstadoVenta?.Nombre ?? (v.EstadoVentaId == 2 ? "Confirmada" : (v.EstadoVentaId == 3 ? "Anulada" : "Pendiente")));

            return new VentasDto
            {
                VentaId = v.VentaId,
                ClienteId = v.ClienteId,
                Cliente = v.Cliente?.Nombre ?? "Cliente general",
                FechaVenta = v.FechaVenta,
                Total = v.Total,
                EstadoVenta = estadoTexto,
                EsCotizacion = v.EsCotizacion,
                NumeroCotizacion = v.NumeroCotizacion,
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
