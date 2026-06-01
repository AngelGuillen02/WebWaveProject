using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Ventas.Dtos;
using SistemaVisionTech.Features.Ventas.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VentasController : BaseApiController
    {
        private readonly IVentasService _ventasService;

        public VentasController(IVentasService ventasService)
        {
            _ventasService = ventasService;
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerVentas()
        {
            Result<IEnumerable<VentasDto>> resultado = await _ventasService.ObtenerVentasAsync();
            return HandleResult(resultado);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerVentaPorId(int id)
        {
            Result<VentasDto> resultado = await _ventasService.ObtenerVentaPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CrearVenta([FromBody] CrearVentaDto dto)
        {
            Result<VentasDto> resultado = await _ventasService.CrearVentaAsync(dto);
            if (resultado.Success && resultado.Data != null)
            {
                return CreatedAtAction(
                    nameof(ObtenerVentaPorId),
                    new { id = resultado.Data.VentaId },
                    resultado.Data);
            }

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }

        [HttpPut("{id:int}/Confirmar")]
        public async Task<IActionResult> ConfirmarVenta(int id)
        {
            Result<VentasDto> resultado = await _ventasService.ConfirmarVentaAsync(id);
            return HandleResult(resultado);
        }

        [HttpPut("{id:int}/Anular")]
        public async Task<IActionResult> AnularVenta(int id)
        {
            Result<VentasDto> resultado = await _ventasService.AnularVentaAsync(id);
            return HandleResult(resultado);
        }

        [HttpPost("Pago")]
        public async Task<IActionResult> RegistrarPago([FromBody] CrearPagoVentaDto dto)
        {
            Result<PagoVentaDto> resultado = await _ventasService.RegistrarPagoAsync(dto);

            if (resultado.Success)
                return StatusCode(201, resultado.Data);

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }
    }
}