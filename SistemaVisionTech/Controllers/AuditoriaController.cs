using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Infrastructure;
using System.Security.Claims;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditoriaController : BaseApiController
    {
        private readonly WebWaveDbContext _context;

        public AuditoriaController(WebWaveDbContext context)
        {
            _context = context;
        }

        private static string ExtraerNombreProducto(string? detalle, int productoId)
        {
            if (!string.IsNullOrWhiteSpace(detalle))
            {
                var match = System.Text.RegularExpressions.Regex.Match(detalle, @"Producto '([^']+)'");
                if (match.Success)
                    return match.Groups[1].Value;
            }
            return "Producto #" + productoId;
        }

        private bool EsSuperUsuario()
        {
            var rolClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
            var perfilIdClaim = User.Claims.FirstOrDefault(c => c.Type == "PerfilId")?.Value;
            return string.Equals(rolClaim, "SuperUsuario", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   perfilIdClaim == "1" ||
                   (rolClaim != null && (rolClaim.Contains("Admin", StringComparison.OrdinalIgnoreCase) || rolClaim.Contains("Super", StringComparison.OrdinalIgnoreCase)));
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerBitacoraCompleta()
        {
            if (!EsSuperUsuario())
            {
                return NotFound();
            }

            // 1. Logs de productos
            var logsProductos = await _context.ProductosHistorial
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaModificacion)
                .Take(200)
                .ToListAsync();

            var logsProductosFormateados = logsProductos.Select(p => new
            {
                id = p.Id,
                tipo = "PRODUCTO",
                entidadId = p.ProductoId,
                referencia = (p.Producto != null && !string.IsNullOrWhiteSpace(p.Producto.Nombre)) 
                    ? p.Producto.Nombre 
                    : ExtraerNombreProducto(p.DetalleCambios, p.ProductoId),
                usuario = p.Usuario,
                fecha = DateTime.SpecifyKind(p.FechaModificacion, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = p.Accion,
                detalle = p.DetalleCambios,
                valorAnterior = p.PrecioAnterior != null ? ("L. " + p.PrecioAnterior.Value.ToString("N2")) : "-",
                valorNuevo = p.Accion.Contains("ELIMIN") ? "Inactivo (Eliminado)" : ("L. " + p.PrecioNuevo.ToString("N2")),
                motivo = p.Accion.Contains("ELIMIN") ? "Eliminación de producto" : "Gestión de catálogo"
            }).ToList();

            // 2. Logs de cotizaciones y facturas
            var logsCotizaciones = await _context.CotizacionesHistorial
                .AsNoTracking()
                .Include(c => c.Venta)
                    .ThenInclude(v => v!.Cliente)
                .OrderByDescending(c => c.FechaModificacion)
                .Take(250)
                .ToListAsync();

            var logsCotizacionesFormateados = logsCotizaciones.Select(c => new
            {
                id = c.Id,
                tipo = c.Accion.Contains("FACTURA") || c.Accion.Contains("SAR") || c.Accion == "FACTURADA" ? "FACTURA SAR" : "COTIZACIÓN",
                entidadId = c.VentaId,
                referencia = c.NumeroCotizacion ?? (c.Venta != null && c.Venta.NumeroCotizacion != null ? c.Venta.NumeroCotizacion : "Doc #" + c.VentaId),
                usuario = c.Usuario,
                fecha = DateTime.SpecifyKind(c.FechaModificacion, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = c.Accion,
                detalle = c.DetalleCambios,
                valorAnterior = c.TotalAnterior != null ? ("L. " + c.TotalAnterior.Value.ToString("N2")) : "-",
                valorNuevo = "L. " + c.TotalNuevo.ToString("N2"),
                motivo = c.Motivo ?? "-"
            }).ToList();

            // 3. Facturas SAR históricas emitidas directamente
            var ventaIdsConLog = logsCotizaciones.Select(l => l.VentaId).ToHashSet();
            var logsFacturas = await _context.FacturasEmitidas
                .AsNoTracking()
                .Where(f => !ventaIdsConLog.Contains(f.VentaId))
                .OrderByDescending(f => f.FechaEmision)
                .Take(150)
                .ToListAsync();

            var logsFacturasFormateados = logsFacturas.Select(f => new
            {
                id = f.Id,
                tipo = "FACTURA SAR",
                entidadId = f.VentaId,
                referencia = f.NumeroFactura,
                usuario = "Facturador SAR",
                fecha = DateTime.SpecifyKind(f.FechaEmision, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = f.Activo ? "EMISIÓN FISCAL" : "ANULADA",
                detalle = "CAI: " + f.CAI + " | Cliente: " + f.NombreCliente + " (RTN: " + (f.RTNCliente ?? "Consumidor Final") + ")",
                valorAnterior = "-",
                valorNuevo = "L. " + f.Total.ToString("N2"),
                motivo = "Emisión fiscal autorizada"
            }).ToList();

            // 4. Logs de Cierres y Arqueos de Caja
            var logsCaja = await _context.CierresCaja
                .AsNoTracking()
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaApertura)
                .Take(150)
                .ToListAsync();

            var logsCajaFormateados = logsCaja.Select(c => new
            {
                id = c.Id,
                tipo = "CAJA",
                entidadId = c.Id,
                referencia = "Turno Caja #" + c.Id,
                usuario = c.Usuario != null ? c.Usuario.Nombre : ("Cajero #" + c.UsuarioId),
                fecha = DateTime.SpecifyKind(c.FechaCierre ?? c.FechaApertura, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = c.Estado == "Abierto" ? "APERTURA CAJA" : "CIERRE Y ARQUEO",
                detalle = c.Estado == "Abierto"
                    ? $"Apertura de caja con fondo inicial de L. {c.MontoApertura:N2}."
                    : $"Cierre de caja. Efectivo físico: L. {(c.MontoEfectivoFinal ?? 0):N2} | Ventas en efectivo: L. {c.TotalVentasEfectivo:N2} | Diferencia: L. {(c.Diferencia ?? 0):N2}.",
                valorAnterior = $"Fondo: L. {c.MontoApertura:N2}",
                valorNuevo = $"Arqueo: L. {(c.MontoEfectivoFinal ?? c.MontoApertura):N2}",
                motivo = (c.Diferencia ?? 0) == 0 ? "Arqueo cuadrado sin diferencia" : ((c.Diferencia ?? 0) > 0 ? $"Sobrante de L. {c.Diferencia:N2}" : $"Faltante de L. {Math.Abs(c.Diferencia ?? 0):N2}")
            }).ToList();

            // 5. Logs de Compras a Proveedores
            var logsCompras = await _context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .OrderByDescending(c => c.FechaCompra)
                .Take(150)
                .ToListAsync();

            var logsComprasFormateados = logsCompras.Select(c => new
            {
                id = c.CompraId,
                tipo = "COMPRA",
                entidadId = c.CompraId,
                referencia = $"OC-#{c.CompraId:D5}",
                usuario = "Gestión Almacén",
                fecha = DateTime.SpecifyKind(c.FechaCompra, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = c.EstadoCompraId == 3 ? "COMPRA ANULADA" : (c.EstadoCompraId == 2 ? "MERCADERÍA RECIBIDA" : "ORDEN DE COMPRA"),
                detalle = $"Proveedor: {c.Proveedor?.Nombre ?? "Proveedor"} | Total de orden ingresado a stock.",
                valorAnterior = "-",
                valorNuevo = "L. " + c.Total.ToString("N2"),
                motivo = c.EstadoCompraId == 3 ? "Anulación de orden de compra" : "Reabastecimiento de inventario"
            }).ToList();

            // 6. Logs de Kardex y Movimientos de Inventario
            var logsKardex = await _context.HistorialMovimientoInventario
                .AsNoTracking()
                .Include(m => m.Inventario)
                    .ThenInclude(i => i!.Producto)
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(150)
                .ToListAsync();

            // Mapa auxiliar para asociar movimientos de venta/cotización a su usuario real
            var ventasUsuariosMap = logsCotizaciones
                .Where(c => c.VentaId > 0 && !string.IsNullOrWhiteSpace(c.Usuario))
                .GroupBy(c => c.VentaId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.FechaModificacion).First().Usuario);

            var logsKardexFormateados = logsKardex.Select(m => {
                string usr = !string.IsNullOrWhiteSpace(m.Usuario) ? m.Usuario : string.Empty;
                if (string.IsNullOrWhiteSpace(usr) && m.OrigenId.HasValue && ventasUsuariosMap.TryGetValue(m.OrigenId.Value, out var uFromMap))
                {
                    usr = uFromMap;
                }
                if (string.IsNullOrWhiteSpace(usr) && string.Equals(m.OrigenTipo, "Venta", StringComparison.OrdinalIgnoreCase))
                {
                    // Buscar usuario de venta emitida en ventana de tiempo cercana (+/- 10 min)
                    var ventaCercana = logsCotizaciones
                        .Where(c => !string.IsNullOrWhiteSpace(c.Usuario) && Math.Abs((c.FechaModificacion - m.FechaMovimiento).TotalMinutes) <= 10)
                        .OrderBy(c => Math.Abs((c.FechaModificacion - m.FechaMovimiento).TotalSeconds))
                        .FirstOrDefault();
                    usr = ventaCercana?.Usuario ?? "Facturación y Ventas";
                }
                if (string.IsNullOrWhiteSpace(usr))
                {
                    usr = "Control Stock";
                }

                return new
                {
                    id = m.MovimientoId,
                    tipo = "KARDEX",
                    entidadId = m.InventarioId,
                    referencia = m.Inventario != null && m.Inventario.Producto != null ? m.Inventario.Producto.Nombre : ($"Inventario #{m.InventarioId}"),
                    usuario = usr,
                    fecha = DateTime.SpecifyKind(m.FechaMovimiento, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    accion = $"KARDEX {m.TipoMovimiento.ToUpper()}",
                    detalle = $"Movimiento de Kardex: {m.TipoMovimiento} de {m.Cantidad} unidades en inventario. Origen: {m.OrigenTipo ?? "Sistema"}.",
                    valorAnterior = "-",
                    valorNuevo = $"{m.Cantidad} unds",
                    motivo = $"Ajuste/Movimiento de stock {m.TipoMovimiento}"
                };
            }).ToList();

            // 7. Logs generales de Auditoría (Clientes, Proveedores, Usuarios, Perfiles, etc.)
            var logsAuditoria = await _context.Auditoria
                .IgnoreQueryFilters()
                .AsNoTracking()
                .OrderByDescending(a => a.Fecha)
                .Take(500)
                .ToListAsync();

            var logsAuditoriaFormateados = logsAuditoria.Select(a => new
            {
                id = a.Id,
                tipo = a.Tipo,
                entidadId = a.EntidadId,
                referencia = a.Referencia,
                usuario = a.Usuario,
                fecha = DateTime.SpecifyKind(a.Fecha, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = a.Accion,
                detalle = a.Detalle,
                valorAnterior = a.ValorAnterior ?? "-",
                valorNuevo = a.ValorNuevo ?? "-",
                motivo = a.Motivo ?? "-"
            }).ToList();

            var combinado = logsProductosFormateados.Cast<object>()
                .Concat(logsCotizacionesFormateados.Cast<object>())
                .Concat(logsFacturasFormateados.Cast<object>())
                .Concat(logsCajaFormateados.Cast<object>())
                .Concat(logsComprasFormateados.Cast<object>())
                .Concat(logsKardexFormateados.Cast<object>())
                .Concat(logsAuditoriaFormateados.Cast<object>())
                .OrderByDescending(x => DateTime.Parse(((dynamic)x).fecha))
                .Take(800)
                .ToList();

            return Ok(combinado);
        }

        [HttpGet("historial")]
        public async Task<IActionResult> ObtenerHistorialEntidad([FromQuery] string? tipo, [FromQuery] int? entidadId, [FromQuery] string? referencia)
        {
            if (!EsSuperUsuario())
            {
                return NotFound();
            }

            tipo = (tipo ?? string.Empty).Trim().ToUpperInvariant();
            referencia = (referencia ?? string.Empty).Trim();

            // Si es Cliente, Proveedor, Usuario o Perfil
            if (tipo.Contains("CLIENT") || tipo.Contains("PROV") || tipo.Contains("USUAR") || tipo.Contains("PERFIL"))
            {
                var queryAud = _context.Auditoria
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(tipo))
                    queryAud = queryAud.Where(a => a.Tipo.Contains(tipo) || tipo.Contains(a.Tipo));

                if (entidadId.HasValue && entidadId.Value > 0)
                    queryAud = queryAud.Where(a => a.EntidadId == entidadId.Value);

                if (!string.IsNullOrEmpty(referencia))
                    queryAud = queryAud.Where(a => a.Referencia.Contains(referencia) || a.Detalle.Contains(referencia));

                var listAud = await queryAud
                    .OrderByDescending(a => a.Fecha)
                    .Take(100)
                    .ToListAsync();

                var listAudFormat = listAud.Select(a => new
                {
                    id = a.Id,
                    tipo = a.Tipo,
                    entidadId = a.EntidadId,
                    referencia = a.Referencia,
                    usuario = a.Usuario,
                    fecha = DateTime.SpecifyKind(a.Fecha, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    accion = a.Accion,
                    detalle = a.Detalle,
                    valorAnterior = a.ValorAnterior ?? "-",
                    valorNuevo = a.ValorNuevo ?? "-",
                    motivo = a.Motivo ?? "-"
                }).ToList();

                return Ok(listAudFormat);
            }

            // Si es Producto
            if (tipo.Contains("PRODUCT") || (!tipo.Contains("FACTURA") && !tipo.Contains("COTIZ") && entidadId.HasValue && entidadId.Value > 0 && !referencia.StartsWith("COT-") && !referencia.StartsWith("000-")))
            {
                var queryProd = _context.ProductosHistorial
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(p => p.Producto)
                    .AsQueryable();

                if (entidadId.HasValue && entidadId.Value > 0)
                    queryProd = queryProd.Where(p => p.ProductoId == entidadId.Value);
                else if (!string.IsNullOrEmpty(referencia))
                    queryProd = queryProd.Where(p => (p.Producto != null && p.Producto.Nombre.Contains(referencia)) || (p.DetalleCambios != null && p.DetalleCambios.Contains(referencia)));

                var logsP = await queryProd
                    .OrderByDescending(p => p.FechaModificacion)
                    .Select(p => new
                    {
                        id = p.Id,
                        tipo = "PRODUCTO",
                        entidadId = p.ProductoId,
                        referencia = (p.Producto != null && !string.IsNullOrWhiteSpace(p.Producto.Nombre)) ? p.Producto.Nombre : ExtraerNombreProducto(p.DetalleCambios, p.ProductoId),
                        usuario = p.Usuario,
                        fecha = p.FechaModificacion,
                        accion = p.Accion,
                        detalle = p.DetalleCambios,
                        valorAnterior = p.PrecioAnterior != null ? ("L. " + p.PrecioAnterior.Value.ToString("N2")) : "-",
                        valorNuevo = "L. " + p.PrecioNuevo.ToString("N2"),
                        motivo = "Gestión de catálogo"
                    })
                    .ToListAsync();

                var logsPFormat = logsP.Select(p => new {
                    p.id, p.tipo, p.entidadId, p.referencia, p.usuario,
                    fecha = DateTime.SpecifyKind(p.fecha, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    p.accion, p.detalle, p.valorAnterior, p.valorNuevo, p.motivo
                }).ToList();
                return Ok(logsPFormat);
            }

            // Si es Caja
            if (tipo.Contains("CAJA"))
            {
                var queryCaja = _context.CierresCaja
                    .AsNoTracking()
                    .Include(c => c.Usuario)
                    .AsQueryable();

                if (entidadId.HasValue && entidadId.Value > 0)
                    queryCaja = queryCaja.Where(c => c.Id == entidadId.Value);

                var listCaja = await queryCaja
                    .OrderByDescending(c => c.FechaApertura)
                    .Take(50)
                    .ToListAsync();

                var listCajaFormat = listCaja.Select(c => new
                {
                    id = c.Id,
                    tipo = "CAJA",
                    entidadId = c.Id,
                    referencia = "Turno Caja #" + c.Id,
                    usuario = c.Usuario != null ? c.Usuario.Nombre : ("Cajero #" + c.UsuarioId),
                    fecha = DateTime.SpecifyKind(c.FechaCierre ?? c.FechaApertura, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    accion = c.Estado == "Abierto" ? "APERTURA CAJA" : "CIERRE Y ARQUEO",
                    detalle = c.Estado == "Abierto"
                        ? $"Apertura de turno operativo con fondo de L. {c.MontoApertura:N2}."
                        : $"Cierre de caja. Efectivo físico arqueado: L. {(c.MontoEfectivoFinal ?? 0):N2} | Ventas en efectivo: L. {c.TotalVentasEfectivo:N2} | Diferencia calculada: L. {(c.Diferencia ?? 0):N2}.",
                    valorAnterior = $"Fondo: L. {c.MontoApertura:N2}",
                    valorNuevo = $"Arqueo: L. {(c.MontoEfectivoFinal ?? c.MontoApertura):N2}",
                    motivo = (c.Diferencia ?? 0) == 0 ? "Cuadratura conforme" : ((c.Diferencia ?? 0) > 0 ? "Sobrante de caja" : "Faltante de caja")
                }).ToList();
                return Ok(listCajaFormat);
            }

            // Si es Compra
            if (tipo.Contains("COMPRA"))
            {
                var queryComp = _context.Compras
                    .AsNoTracking()
                    .Include(c => c.Proveedor)
                    .Include(c => c.Detalles)
                    .AsQueryable();

                if (entidadId.HasValue && entidadId.Value > 0)
                    queryComp = queryComp.Where(c => c.CompraId == entidadId.Value);

                var listComp = await queryComp
                    .OrderByDescending(c => c.FechaCompra)
                    .Take(50)
                    .ToListAsync();

                var listCompFormat = listComp.Select(c => new
                {
                    id = c.CompraId,
                    tipo = "COMPRA",
                    entidadId = c.CompraId,
                    referencia = $"OC-#{c.CompraId:D5}",
                    usuario = "Almacén / Compras",
                    fecha = DateTime.SpecifyKind(c.FechaCompra, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    accion = c.EstadoCompraId == 3 ? "COMPRA ANULADA" : (c.EstadoCompraId == 2 ? "MERCADERÍA RECIBIDA" : "ORDEN REGISTRADA"),
                    detalle = $"Proveedor: {c.Proveedor?.Nombre ?? "Proveedor"} | {c.Detalles.Count} productos registrados en orden.",
                    valorAnterior = "-",
                    valorNuevo = "L. " + c.Total.ToString("N2"),
                    motivo = c.EstadoCompraId == 3 ? "Anulación de orden" : "Ingreso de mercadería"
                }).ToList();
                return Ok(listCompFormat);
            }

            // Si es Kardex
            if (tipo.Contains("KARDEX"))
            {
                var queryK = _context.HistorialMovimientoInventario
                    .AsNoTracking()
                    .Include(m => m.Inventario)
                        .ThenInclude(i => i!.Producto)
                    .AsQueryable();

                if (entidadId.HasValue && entidadId.Value > 0)
                    queryK = queryK.Where(m => m.InventarioId == entidadId.Value || m.MovimientoId == entidadId.Value);

                var listK = await queryK
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(50)
                    .ToListAsync();

                var listKFormat = listK.Select(m => {
                    string usr = !string.IsNullOrWhiteSpace(m.Usuario) ? m.Usuario : (string.Equals(m.OrigenTipo, "Venta", StringComparison.OrdinalIgnoreCase) ? "Facturación y Ventas" : "Control Stock");
                    return new
                    {
                        id = m.MovimientoId,
                        tipo = "KARDEX",
                        entidadId = m.InventarioId,
                        referencia = m.Inventario != null && m.Inventario.Producto != null ? m.Inventario.Producto.Nombre : ($"Inventario #{m.InventarioId}"),
                        usuario = usr,
                        fecha = DateTime.SpecifyKind(m.FechaMovimiento, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        accion = $"KARDEX {m.TipoMovimiento.ToUpper()}",
                        detalle = $"Kardex: {m.TipoMovimiento} de {m.Cantidad} unidades. Origen: {m.OrigenTipo ?? "Sistema"}.",
                        valorAnterior = "-",
                        valorNuevo = $"{m.Cantidad} unds",
                        motivo = $"Auditoría de inventario {m.TipoMovimiento}"
                    };
                }).ToList();
                return Ok(listKFormat);
            }

            // Si es Cotización o Factura
            var queryCot = _context.CotizacionesHistorial
                .AsNoTracking()
                .Include(c => c.Venta)
                .AsQueryable();

            if (entidadId.HasValue && entidadId.Value > 0)
            {
                queryCot = queryCot.Where(c => c.VentaId == entidadId.Value);
            }
            else if (!string.IsNullOrEmpty(referencia))
            {
                queryCot = queryCot.Where(c => c.NumeroCotizacion == referencia || (c.Venta != null && c.Venta.NumeroCotizacion == referencia));
            }

            var logsV = await queryCot
                .OrderByDescending(c => c.FechaModificacion)
                .Select(c => new
                {
                    id = c.Id,
                    tipo = c.Accion.Contains("FACTURA") || c.Accion.Contains("SAR") || c.Accion == "FACTURADA" ? "FACTURA SAR" : "COTIZACIÓN",
                    entidadId = c.VentaId,
                    referencia = c.NumeroCotizacion ?? (c.Venta != null && c.Venta.NumeroCotizacion != null ? c.Venta.NumeroCotizacion : "Doc #" + c.VentaId),
                    usuario = c.Usuario,
                    fecha = c.FechaModificacion,
                    accion = c.Accion,
                    detalle = c.DetalleCambios,
                    valorAnterior = c.TotalAnterior != null ? ("L. " + c.TotalAnterior.Value.ToString("N2")) : "-",
                    valorNuevo = "L. " + c.TotalNuevo.ToString("N2"),
                    motivo = c.Motivo ?? "-"
                })
                .ToListAsync();

            // Si no hubo entradas en CotizacionesHistorial pero sí en FacturasEmitidas
            if (!logsV.Any() && (!string.IsNullOrEmpty(referencia) || entidadId.HasValue))
            {
                var queryFac = _context.FacturasEmitidas.AsNoTracking().AsQueryable();
                if (entidadId.HasValue) queryFac = queryFac.Where(f => f.VentaId == entidadId.Value);
                if (!string.IsNullOrEmpty(referencia)) queryFac = queryFac.Where(f => f.NumeroFactura == referencia);

                var logsF = await queryFac
                    .Select(f => new
                    {
                        id = f.Id,
                        tipo = "FACTURA SAR",
                        entidadId = f.VentaId,
                        referencia = f.NumeroFactura,
                        usuario = "Facturador SAR",
                        fecha = f.FechaEmision,
                        accion = f.Activo ? "EMISIÓN FISCAL" : "ANULADA",
                        detalle = "CAI: " + f.CAI + " | Cliente: " + f.NombreCliente + " (RTN: " + (f.RTNCliente ?? "Consumidor Final") + ")",
                        valorAnterior = "-",
                        valorNuevo = "L. " + f.Total.ToString("N2"),
                        motivo = "Emisión fiscal autorizada"
                    })
                    .ToListAsync();

                var logsFFormat = logsF.Select(f => new {
                    f.id, f.tipo, f.entidadId, f.referencia, f.usuario,
                    fecha = DateTime.SpecifyKind(f.fecha, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    f.accion, f.detalle, f.valorAnterior, f.valorNuevo, f.motivo
                }).ToList();
                return Ok(logsFFormat);
            }

            var logsVFormat = logsV.Select(c => new {
                c.id, c.tipo, c.entidadId, c.referencia, c.usuario,
                fecha = DateTime.SpecifyKind(c.fecha, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                c.accion, c.detalle, c.valorAnterior, c.valorNuevo, c.motivo
            }).ToList();
            return Ok(logsVFormat);
        }

        [HttpPost("registrar-evento")]
        public async Task<IActionResult> RegistrarEvento([FromBody] RegistrarLogAuditoriaDto dto)
        {
            if (dto == null || dto.EntidadId <= 0)
            {
                return BadRequest(new { mensaje = "Datos de auditoría incompletos." });
            }

            // Seguridad [VULN-04]: La identidad en bitácora DEBE provenir siempre de los claims del JWT firmado
            var usuario = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value
                ?? User.Identity?.Name
                ?? "Usuario Autenticado";

            string accionNormalizada = (dto.Accion ?? "IMPRESIÓN").Trim().ToUpperInvariant();
            string detalle = !string.IsNullOrWhiteSpace(dto.Detalle)
                ? dto.Detalle.Trim()
                : $"Acción de {accionNormalizada} sobre el documento {dto.Referencia ?? ("#" + dto.EntidadId)}.";

            if (!string.IsNullOrWhiteSpace(dto.Tipo) && !dto.Tipo.Contains("COTIZ") && !dto.Tipo.Contains("FACTURA"))
            {
                var auditEvent = new SistemaVisionTech.Infrastructure.Entities.Auditoria
                {
                    Tipo = dto.Tipo.ToUpperInvariant(),
                    EntidadId = dto.EntidadId,
                    Referencia = dto.Referencia ?? ($"Registro #{dto.EntidadId}"),
                    Usuario = usuario,
                    Fecha = DateTime.UtcNow,
                    Accion = accionNormalizada,
                    Detalle = detalle,
                    ValorAnterior = "-",
                    ValorNuevo = dto.Total.HasValue ? ("L. " + dto.Total.Value.ToString("N2")) : "-",
                    Motivo = dto.Motivo ?? "Gestión y auditoría del sistema",
                    Activo = true
                };
                _context.Auditoria.Add(auditEvent);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Evento registrado exitosamente en bitácora." });
            }

            var log = new SistemaVisionTech.Infrastructure.Entities.CotizacionesHistorial
            {
                VentaId = dto.EntidadId,
                NumeroCotizacion = dto.Referencia,
                Usuario = usuario,
                FechaModificacion = DateTime.UtcNow,
                Accion = accionNormalizada,
                DetalleCambios = detalle,
                TotalAnterior = dto.Total,
                TotalNuevo = dto.Total ?? 0,
                Motivo = dto.Motivo ?? "Gestión de comprobante oficial",
                Activo = true
            };

            _context.CotizacionesHistorial.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Evento registrado exitosamente en bitácora." });
        }
    }

    public class RegistrarLogAuditoriaDto
    {
        public string? Tipo { get; set; }
        public int EntidadId { get; set; }
        public string? Referencia { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? Motivo { get; set; }
        public decimal? Total { get; set; }
        public string? Usuario { get; set; }
    }
}
