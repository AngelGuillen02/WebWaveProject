using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Interfaces;
using System.Security.Claims;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresController : BaseApiController
    {
        private readonly IProveedoresService _svc;

        public ProveedoresController(IProveedoresService svc)
        {
            _svc = svc;
        }

        private string ObtenerUsuarioActual()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Administrador";
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
            => HandleResult(await _svc.ListarAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
            => HandleResult(await _svc.ObtenerPorIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProveedorCreacionDto dto)
            => HandleCreatedResult(
                await _svc.CrearAsync(dto, ObtenerUsuarioActual()),
                nameof(ObtenerPorId),
                r => new { id = r.ProveedorId });
        [HttpPost("Importar")]
        public async Task<IActionResult> ImportarLote([FromBody] List<ProveedorCreacionDto> dtos)
            => HandleResult(await _svc.ImportarLoteAsync(dtos, ObtenerUsuarioActual()));

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ProveedorCreacionDto dto)
            => HandleResult(await _svc.EditarAsync(id, dto, ObtenerUsuarioActual()));
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
            => HandleResult(await _svc.EliminarAsync(id, ObtenerUsuarioActual()));
    }
}
