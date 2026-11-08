using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Inventario.Dtos.Inventario;
using SistemaVisionTech.Features.Inventario.Dtos.Movimientos;
using SistemaVisionTech.Features.Inventario.Interfaces;

using System.Security.Claims;

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

        private string ObtenerUsuarioActual()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Control Stock";
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerInventario()
        {
            Result<IEnumerable<InventarioDto>> resultado = await _inventarioService.ObtenerInventarioAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Producto/{productoId:int}")]
        public async Task<IActionResult> ObtenerPorProducto(int productoId)
        {
            Result<InventarioDto> resultado = await _inventarioService
                .ObtenerInventarioPorProductoAsync(productoId);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,EncargadoInventario,Almacén e Inventario,Almacen")]
        [HttpPost("Movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] CrearMovimientoInventarioDto dto)
        {
            Result<MovimientoDto> resultado = await _inventarioService
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
            Result<IEnumerable<MovimientoDto>> resultado = await _inventarioService
                .ObtenerMovimientosAsync(productoId);
            return HandleResult(resultado);
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,EncargadoInventario,Almacén e Inventario,Almacen")]
        [HttpPut("Ajuste")]
        public async Task<IActionResult> AjustarInventario([FromBody] AjusteInventarioDto dto)
        {
            Result<InventarioDto> resultado = await _inventarioService
                .AjustarInventarioAsync(dto, ObtenerUsuarioActual());
            return HandleResult(resultado);
        }
    }
}