using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Trazabilidad.Dtos;
using SistemaVisionTech.Features.Trazabilidad.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TrazabilidadController : BaseApiController
    {
        private readonly ISeriesProductoService _seriesService;
        private readonly ILotesProductoService _lotesService;

        public TrazabilidadController(ISeriesProductoService seriesService, ILotesProductoService lotesService)
        {
            _seriesService = seriesService;
            _lotesService = lotesService;
        }

        [HttpGet("series")]
        public async Task<IActionResult> ListarSeries()
            => HandleResult(await _seriesService.ListarAsync());

        [HttpGet("series/{id}")]
        public async Task<IActionResult> ObtenerSeriePorId(int id)
            => HandleResult(await _seriesService.ObtenerPorIdAsync(id));

        [HttpPost("series")]
        public async Task<IActionResult> CrearSerie([FromBody] SerieProductoCreacionDto dto)
            => HandleCreatedResult(
                await _seriesService.CrearAsync(dto),
                nameof(ObtenerSeriePorId),
                r => new { id = r.Id });

        [HttpDelete("series/{id}")]
        public async Task<IActionResult> EliminarSerie(int id)
            => HandleResult(await _seriesService.EliminarAsync(id));

        [HttpGet("lotes")]
        public async Task<IActionResult> ListarLotes()
            => HandleResult(await _lotesService.ListarAsync());

        [HttpGet("lotes/{id}")]
        public async Task<IActionResult> ObtenerLotePorId(int id)
            => HandleResult(await _lotesService.ObtenerPorIdAsync(id));

        [HttpPost("lotes")]
        public async Task<IActionResult> CrearLote([FromBody] LoteProductoCreacionDto dto)
            => HandleCreatedResult(
                await _lotesService.CrearAsync(dto),
                nameof(ObtenerLotePorId),
                r => new { id = r.Id });

        [HttpPut("lotes/{id}")]
        public async Task<IActionResult> EditarLote(int id, [FromBody] LoteProductoCreacionDto dto)
            => HandleResult(await _lotesService.EditarAsync(id, dto));

        [HttpDelete("lotes/{id}")]
        public async Task<IActionResult> EliminarLote(int id)
            => HandleResult(await _lotesService.EliminarAsync(id));
    }
}
