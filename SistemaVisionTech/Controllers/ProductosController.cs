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
            => HandleResult(await _svc.EditarAsync(id, dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
            => HandleResult(await _svc.EliminarAsync(id));
    }
}
