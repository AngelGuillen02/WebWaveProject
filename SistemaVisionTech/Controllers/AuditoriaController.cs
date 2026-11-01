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

        private bool EsAdmin()
        {
            var rolClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var perfilIdClaim = User.Claims.FirstOrDefault(c => c.Type == "PerfilId")?.Value;
            return string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   perfilIdClaim == "1";
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerBitacoraCompleta()
        {
            if (!EsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    mensaje = "Acceso denegado: Únicamente los usuarios administradores pueden consultar la bitácora de auditoría global."
                });
            }

            // 1. Logs de productos
            var logsProductos = await _context.ProductosHistorial
                .AsNoTracking()
                .Include(p => p.Producto)
                .OrderByDescending(p => p.FechaModificacion)
                .Take(200)
                .Select(p => new
                {
                    tipo = "PRODUCTO",
                    referencia = p.Producto != null ? p.Producto.Nombre : ("Producto #" + p.ProductoId),
                    usuario = p.Usuario,
                    fecha = p.FechaModificacion,
                    accion = p.Accion,
                    detalle = p.DetalleCambios,
                    valorAnterior = p.PrecioAnterior != null ? ("L. " + p.PrecioAnterior.ToString()) : "-",
                    valorNuevo = "L. " + p.PrecioNuevo.ToString()
                })
                .ToListAsync();

            // 2. Logs de cotizaciones
            var logsCotizaciones = await _context.CotizacionesHistorial
                .AsNoTracking()
                .OrderByDescending(c => c.FechaModificacion)
                .Take(200)
                .Select(c => new
                {
                    tipo = "COTIZACIÓN",
                    referencia = c.NumeroCotizacion ?? ("Cotización #" + c.VentaId),
                    usuario = c.Usuario,
                    fecha = c.FechaModificacion,
                    accion = c.Accion,
                    detalle = c.DetalleCambios,
                    valorAnterior = "L. " + c.TotalAnterior.ToString(),
                    valorNuevo = "L. " + c.TotalNuevo.ToString()
                })
                .ToListAsync();

            // 3. Logs de facturación fiscal SAR
            var logsFacturas = await _context.FacturasEmitidas
                .AsNoTracking()
                .OrderByDescending(f => f.FechaEmision)
                .Take(200)
                .Select(f => new
                {
                    tipo = "FACTURA SAR",
                    referencia = f.NumeroFactura,
                    usuario = f.NombreCliente,
                    fecha = f.FechaEmision,
                    accion = f.Activo ? "EMISIÓN FISCAL" : "ANULADA",
                    detalle = "CAI: " + f.CAI + " | Cliente: " + f.NombreCliente + " (RTN: " + (f.RTNCliente ?? "Consumidor Final") + ")",
                    valorAnterior = "-",
                    valorNuevo = "L. " + f.Total.ToString()
                })
                .ToListAsync();

            var combinado = logsProductos.Cast<object>()
                .Concat(logsCotizaciones.Cast<object>())
                .Concat(logsFacturas.Cast<object>())
                .OrderByDescending(x => ((dynamic)x).fecha)
                .Take(400)
                .ToList();

            return Ok(combinado);
        }
    }
}
