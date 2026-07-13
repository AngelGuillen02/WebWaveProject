using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Caja.Dtos;
using SistemaVisionTech.Features.Caja.Interfaces;
using System.Security.Claims;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CajaController : BaseApiController
    {
        private readonly ICajaService _svc;

        public CajaController(ICajaService svc)
        {
            _svc = svc;
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirCaja([FromBody] AbrirCajaDto dto)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            return HandleCreatedResult(await _svc.AbrirCajaAsync(dto, usuarioId), nameof(ObtenerEstadoActual), r => new { id = r.Id });
        }

        [HttpPost("{id}/cerrar")]
        public async Task<IActionResult> CerrarCaja(int id, [FromBody] CerrarCajaDto dto)
            => HandleResult(await _svc.CerrarCajaAsync(id, dto));

        [HttpGet("actual/{sucursalId}")]
        public async Task<IActionResult> ObtenerEstadoActual(int sucursalId)
            => HandleResult(await _svc.ObtenerEstadoActualAsync(sucursalId));

        [HttpGet("historial/{sucursalId}")]
        public async Task<IActionResult> ObtenerHistorial(int sucursalId)
            => HandleResult(await _svc.ObtenerHistorialAsync(sucursalId));
    }
}
