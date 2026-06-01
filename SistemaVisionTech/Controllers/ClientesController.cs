using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Clientes.Dtos;
using SistemaVisionTech.Features.Clientes.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : BaseApiController
    {
        private readonly IClientesService _svc;

        public ClientesController(IClientesService svc)
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
        public async Task<IActionResult> Crear([FromBody] ClienteCreacionDto dto)
            => HandleCreatedResult(
                await _svc.CrearAsync(dto),
                nameof(ObtenerPorId),
                r => new { id = r.ClienteId });

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ClienteCreacionDto dto)
            => HandleResult(await _svc.EditarAsync(id, dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
            => HandleResult(await _svc.EliminarAsync(id));
    }
}
