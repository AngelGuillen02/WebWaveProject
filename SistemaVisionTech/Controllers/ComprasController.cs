using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Dtos.Compras;
using SistemaVisionTech.Features.Compras.Interfeces;

namespace SistemaVisionTech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly IComprasService _comprasService;

        public ComprasController(IComprasService comprasService)
        {
            _comprasService = comprasService;
        }

        // ── Helper para manejar Result<T> ────────────────────────────────

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.Success)
                return Ok(result.Data);

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }

        // GET api/Compras
        [HttpGet]
        public async Task<IActionResult> ObtenerCompras()
        {
            var resultado = await _comprasService.ObtenerComprasAsync();
            return HandleResult(resultado);
        }

        // GET api/Compras/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerCompraPorId(int id)
        {
            var resultado = await _comprasService.ObtenerCompraPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        // POST api/Compras
        [HttpPost]
        public async Task<IActionResult> CrearCompra([FromBody] CrearCompraDto dto)
        {
            var resultado = await _comprasService.CrearCompraAsync(dto);

            if (resultado.Success)
                return CreatedAtAction(
                    nameof(ObtenerCompraPorId),
                    new { id = resultado.Data.CompraId },
                    resultado.Data);

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }

        // PUT api/Compras/5/Recibir
        [HttpPut("{id:int}/Recibir")]
        public async Task<IActionResult> RecibirCompra(int id)
        {
            var resultado = await _comprasService.RecibirCompraAsync(id);
            return HandleResult(resultado);
        }

        // PUT api/Compras/5/Anular
        [HttpPut("{id:int}/Anular")]
        public async Task<IActionResult> AnularCompra(int id)
        {
            var resultado = await _comprasService.AnularCompraAsync(id);
            return HandleResult(resultado);
        }

        // POST api/Compras/Pago
        [HttpPost("Pago")]
        public async Task<IActionResult> RegistrarPago(
            [FromBody] CrearPagoCompraDto dto)
        {
            var resultado = await _comprasService.RegistrarPagoAsync(dto);

            if (resultado.Success)
                return StatusCode(201, resultado.Data);

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }
    }
}