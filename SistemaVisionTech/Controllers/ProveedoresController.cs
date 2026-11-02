using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Interfaces;

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

        [HttpGet]
        public async Task<IActionResult> Listar()
            => HandleResult(await _svc.ListarAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
            => HandleResult(await _svc.ObtenerPorIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProveedorCreacionDto dto)
            => HandleCreatedResult(
                await _svc.CrearAsync(dto),
                nameof(ObtenerPorId),
                r => new { id = r.ProveedorId });

        [HttpPost("Importar")]
        public async Task<IActionResult> ImportarLote([FromBody] List<ProveedorCreacionDto> dtos)
            => HandleResult(await _svc.ImportarLoteAsync(dtos));

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ProveedorCreacionDto dto)
            => HandleResult(await _svc.EditarAsync(id, dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
            => HandleResult(await _svc.EliminarAsync(id));
    }
}
