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

        private bool EsSuperUsuario()
        {
            var rolClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var perfilIdClaim = User.Claims.FirstOrDefault(c => c.Type == "PerfilId")?.Value;
            return string.Equals(rolClaim, "SuperUsuario", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   perfilIdClaim == "1";
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
                referencia = p.Producto != null ? p.Producto.Nombre : ("Producto #" + p.ProductoId),
                usuario = p.Usuario,
                fecha = DateTime.SpecifyKind(p.FechaModificacion, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accion = p.Accion,
                detalle = p.DetalleCambios,
                valorAnterior = p.PrecioAnterior != null ? ("L. " + p.PrecioAnterior.Value.ToString("N2")) : "-",
                valorNuevo = "L. " + p.PrecioNuevo.ToString("N2"),
                motivo = "Gestión de catálogo"
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

            var combinado = logsProductosFormateados.Cast<object>()
                .Concat(logsCotizacionesFormateados.Cast<object>())
                .Concat(logsFacturasFormateados.Cast<object>())
                .OrderByDescending(x => ((dynamic)x).fecha)
                .Take(500)
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

            // Si es Producto
            if (tipo.Contains("PRODUCT") || (!tipo.Contains("FACTURA") && !tipo.Contains("COTIZ") && entidadId.HasValue && entidadId.Value > 0 && !referencia.StartsWith("COT-") && !referencia.StartsWith("000-")))
            {
                var queryProd = _context.ProductosHistorial
                    .AsNoTracking()
                    .Include(p => p.Producto)
                    .AsQueryable();

                if (entidadId.HasValue && entidadId.Value > 0)
                    queryProd = queryProd.Where(p => p.ProductoId == entidadId.Value);
                else if (!string.IsNullOrEmpty(referencia))
                    queryProd = queryProd.Where(p => p.Producto != null && p.Producto.Nombre.Contains(referencia));

                var logsP = await queryProd
                    .OrderByDescending(p => p.FechaModificacion)
                    .Select(p => new
                    {
                        id = p.Id,
                        tipo = "PRODUCTO",
                        entidadId = p.ProductoId,
                        referencia = p.Producto != null ? p.Producto.Nombre : ("Producto #" + p.ProductoId),
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
    }
}
