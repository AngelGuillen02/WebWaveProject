using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Productos.Dtos;
using SistemaVisionTech.Features.Productos.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : BaseApiController
    {
        private readonly IProductosService _svc;

        public ProductosController(IProductosService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
            => HandleResult(await _svc.ListarAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
            => HandleResult(await _svc.ObtenerPorIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProductoCreacionDto dto)
            => HandleCreatedResult(
                await _svc.CrearAsync(dto),
                nameof(ObtenerPorId),
                r => new { id = r.ProductoId });

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ProductoCreacionDto dto)
        {
            string usuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Administrador";
            return HandleResult(await _svc.EditarAsync(id, dto, usuario));
        }

        [HttpGet("{id}/Historial")]
        public async Task<IActionResult> ObtenerHistorial(int id)
        {
            var rolClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var perfilIdClaim = User.Claims.FirstOrDefault(c => c.Type == "PerfilId")?.Value;

            bool esAdmin = string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           perfilIdClaim == "1";

            if (!esAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    mensaje = "Acceso denegado: Únicamente los usuarios con rol de Administrador pueden consultar la bitácora confidencial de auditoría de productos."
                });
            }

            return HandleResult(await _svc.ObtenerHistorialAsync(id));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
            => HandleResult(await _svc.EliminarAsync(id));
    }
}
