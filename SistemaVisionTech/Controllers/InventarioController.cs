using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Inventario.Dtos;
using SistemaVisionTech.Features.Inventario.Dtos.Inventario;
using SistemaVisionTech.Features.Inventario.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InventarioController : BaseApiController
    {
        private readonly IInventarioService _inventarioService;

        public InventarioController(IInventarioService inventarioService)
        {
            _inventarioService = inventarioService;
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerInventario()
        {
            var resultado = await _inventarioService.ObtenerInventarioAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Producto/{productoId:int}")]
        public async Task<IActionResult> ObtenerPorProducto(int productoId)
        {
            var resultado = await _inventarioService
                .ObtenerInventarioPorProductoAsync(productoId);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] CrearMovimientoInventarioDto dto)
        {
            var resultado = await _inventarioService
                .RegistrarMovimientoAsync(dto);

            if (resultado.Success)
                return StatusCode(201, resultado.Data);

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }

        [HttpGet("Movimientos")]
        public async Task<IActionResult> ObtenerMovimientos([FromQuery] int? productoId = null)
        {
            var resultado = await _inventarioService
                .ObtenerMovimientosAsync(productoId);
            return HandleResult(resultado);
        }

        [HttpPut("Ajuste")]
        public async Task<IActionResult> AjustarInventario([FromBody] AjusteInventarioDto dto)
        {
            var resultado = await _inventarioService
                .AjustarInventarioAsync(dto);
            return HandleResult(resultado);
        }
    }
}