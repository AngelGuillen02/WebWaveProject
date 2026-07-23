using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Navegacion.Dtos;
using SistemaVisionTech.Features.Navegacion.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NavegacionController : BaseApiController
    {
        private readonly INavegacionService _svc;

        public NavegacionController(INavegacionService svc)
        {
            _svc = svc;
        }

        // Cualquier usuario autenticado puede pedir SU PROPIO menu,
        // sin importar sus permisos granulares (evita el problema de
        // "necesito Navegacion.Lectura para poder ver que no tengo nada que ver").
        [AllowAnonymous]
        [HttpGet("Menu")]
        public async Task<IActionResult> ObtenerMiMenu()
        {
            string? perfilIdClaim = User.FindFirst("PerfilId")?.Value;
            if (!int.TryParse(perfilIdClaim, out int perfilId))
                return Unauthorized(new { mensaje = "Token inválido o ausente." });

            return HandleResult(await _svc.ObtenerMenuAsync(perfilId));
        }

        [HttpGet("Nodos")]
        public async Task<IActionResult> ObtenerArbol()
            => HandleResult(await _svc.ObtenerArbolAsync());

        [HttpPost("Nodos")]
        public async Task<IActionResult> CrearNodo([FromBody] NavNodoCreacionDto dto)
            => HandleResult(await _svc.CrearNodoAsync(dto));

        [HttpPut("Nodos/{id:int}")]
        public async Task<IActionResult> ActualizarNodo(int id, [FromBody] NavNodoCreacionDto dto)
            => HandleResult(await _svc.ActualizarNodoAsync(id, dto));

        [HttpDelete("Nodos/{id:int}")]
        public async Task<IActionResult> EliminarNodo(int id)
            => HandleResult(await _svc.EliminarNodoAsync(id));

        [HttpPut("Nodos/{id:int}/Perfiles")]
        public async Task<IActionResult> GuardarPerfiles(int id, [FromBody] GuardarPerfilesNodoDto dto)
            => HandleResult(await _svc.GuardarPerfilesDelNodoAsync(id, dto));

        [HttpGet("ArbolConAcceso/{perfilId:int}")]
        public async Task<IActionResult> ObtenerArbolConAcceso(int perfilId)
            => HandleResult(await _svc.ObtenerArbolConAccesoAsync(perfilId));

        [HttpPut("Restricciones/{perfilId:int}")]
        public async Task<IActionResult> GuardarRestriccionesRol(int perfilId, [FromBody] SetRestriccionesRolDto dto)
            => HandleResult(await _svc.GuardarRestriccionesRolAsync(perfilId, dto));
    }
}
