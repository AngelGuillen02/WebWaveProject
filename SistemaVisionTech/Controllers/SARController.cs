using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.SAR.Dtos;
using SistemaVisionTech.Features.SAR.Interfaces;
using System.Security.Claims;

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
        public async Task<IActionResult> GuardarConfiguracion([FromBody] CrearConfiguracionSARDto dto)
        {
            var rolClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var perfilIdClaim = User.Claims.FirstOrDefault(c => c.Type == "PerfilId")?.Value;

            bool esAdmin = string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           perfilIdClaim == "1";

            if (!esAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    mensaje = "Acceso denegado: Únicamente los usuarios con rol de Administrador pueden configurar el CAI y los rangos de facturación del SAR."
                });
            }

            var resultado = await _svc.CrearConfiguracionAsync(dto);
            return HandleResult(resultado);
        }

        [HttpGet("configuracion/{sucursalId:int}")]
        public async Task<IActionResult> ObtenerConfiguracionActual(int sucursalId = 1)
        {
            var resultado = await _svc.ObtenerConfiguracionActualAsync(sucursalId);
            return HandleResult(resultado);
        }

        [HttpPost("emitir-factura/{ventaId:int}/{sucursalId:int}")]
        public async Task<IActionResult> EmitirFactura(int ventaId, int sucursalId = 1)
        {
            var resultado = await _svc.EmitirFacturaAsync(ventaId, sucursalId);
            return HandleResult(resultado);
        }

        [HttpGet("factura/{ventaId:int}")]
        public async Task<IActionResult> ObtenerFactura(int ventaId)
        {
            var resultado = await _svc.ObtenerFacturaAsync(ventaId);
            return HandleResult(resultado);
        }
    }
}
