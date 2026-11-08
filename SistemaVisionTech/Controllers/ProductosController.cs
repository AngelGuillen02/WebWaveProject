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
        {
            string usuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Administrador";

            return HandleCreatedResult(
                await _svc.CrearAsync(dto, usuario),
                nameof(ObtenerPorId),
                r => new { id = r.ProductoId });
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,Super Administrador,SuperAdministrador")]
        [HttpPost("Importar")]
        public async Task<IActionResult> ImportarLote([FromBody] List<ProductoCreacionDto> dtos)
        {
            string usuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Administrador";

            return HandleResult(await _svc.ImportarLoteAsync(dtos, usuario));
        }

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
                return NotFound();

            return HandleResult(await _svc.ObtenerHistorialAsync(id));
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,Super Administrador,SuperAdministrador")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            string usuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Administrador";

            return HandleResult(await _svc.EliminarAsync(id, usuario));
        }
    }
}
