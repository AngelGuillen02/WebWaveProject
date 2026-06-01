using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.SAR.Dtos;
using SistemaVisionTech.Features.SAR.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SARController : BaseApiController
    {
        private readonly ISARService _svc;

        public SARController(ISARService svc)
        {
            _svc = svc;
        }

        [HttpPost("configuracion")]
        public async Task<IActionResult> CrearConfiguracion([FromBody] CrearConfiguracionSARDto dto)
            => HandleCreatedResult(
                await _svc.CrearConfiguracionAsync(dto),
                nameof(ObtenerConfiguracionActual),
                r => new { sucursalId = r.SucursalId });

        [HttpGet("configuracion/{sucursalId}")]
        public async Task<IActionResult> ObtenerConfiguracionActual(int sucursalId)
            => HandleResult(await _svc.ObtenerConfiguracionActualAsync(sucursalId));

        [HttpPost("emitir-factura/{ventaId}/{sucursalId}")]
        public async Task<IActionResult> EmitirFactura(int ventaId, int sucursalId)
            => HandleResult(await _svc.EmitirFacturaAsync(ventaId, sucursalId));

        [HttpGet("factura/{ventaId}")]
        public async Task<IActionResult> ObtenerFactura(int ventaId)
            => HandleResult(await _svc.ObtenerFacturaAsync(ventaId));
    }
}
