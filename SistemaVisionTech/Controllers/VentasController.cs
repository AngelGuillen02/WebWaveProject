using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.SAR.Dtos;
using SistemaVisionTech.Features.SAR.Interfaces;
using SistemaVisionTech.Features.Ventas.Dtos;
using SistemaVisionTech.Features.Ventas.Interfaces;
using System.Security.Claims;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VentasController : BaseApiController
    {
        private readonly IVentasService _ventasService;
        private readonly ISARService _sarService;

        public VentasController(IVentasService ventasService, ISARService sarService)
        {
            _ventasService = ventasService;
            _sarService = sarService;
        }

        private string ObtenerNombreUsuarioActual()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Usuario";
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
            if (string.IsNullOrWhiteSpace(dto.UsuarioNombre))
            {
                dto.UsuarioNombre = ObtenerNombreUsuarioActual();
            }

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

        [HttpPut("{id:int}/Cotizacion")]
        public async Task<IActionResult> ActualizarCotizacion(int id, [FromBody] ActualizarCotizacionDto dto)
        {
            string usuario = ObtenerNombreUsuarioActual();
            Result<VentasDto> resultado = await _ventasService.ActualizarCotizacionAsync(id, dto, usuario);
            return HandleResult(resultado);
        }

        [HttpGet("{id:int}/Cotizacion/Historial")]
        public async Task<IActionResult> ObtenerHistorialCotizacion(int id)
        {
            Result<IEnumerable<CotizacionHistorialDto>> resultado = await _ventasService.ObtenerHistorialCotizacionAsync(id);
            return HandleResult(resultado);
        }

        [HttpPost("{id:int}/FacturarCotizacion")]
        public async Task<IActionResult> FacturarCotizacion(int id, [FromQuery] int sucursalId = 1, [FromQuery] int metodoPagoId = 1)
        {
            string usuario = ObtenerNombreUsuarioActual();
            Result<VentasDto> resultado = await _ventasService.FacturarCotizacionAsync(id, sucursalId, metodoPagoId, usuario);
            return HandleResult(resultado);
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

        [HttpPost("{id:int}/Facturar")]
        public async Task<IActionResult> FacturarVenta(int id, [FromQuery] int sucursalId = 1)
        {
            Result<FacturaEmitidaResponseDto> resultado = await _sarService.EmitirFacturaAsync(id, sucursalId);
            return HandleResult(resultado);
        }

        [HttpGet("{id:int}/Factura")]
        public async Task<IActionResult> ObtenerFactura(int id)
        {
            Result<FacturaEmitidaResponseDto> resultado = await _sarService.ObtenerFacturaAsync(id);
            return HandleResult(resultado);
        }
    }
}
