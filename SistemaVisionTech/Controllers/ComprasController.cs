using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos.Compras;
using SistemaVisionTech.Features.Compras.Dtos.Pagos;
using SistemaVisionTech.Features.Compras.Interfaces;
using System.Security.Claims;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : BaseApiController
    {
        private readonly IComprasService _comprasService;

        public ComprasController(IComprasService comprasService)
        {
            _comprasService = comprasService;
        }

        private string ObtenerUsuarioActual()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value
                ?? "Gestión Almacén";
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCompras()
        {
            Result<IEnumerable<CompraDto>> resultado = await _comprasService.ObtenerComprasAsync();
            return HandleResult(resultado);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerCompraPorId(int id)
        {
            Result<CompraDto> resultado = await _comprasService.ObtenerCompraPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCompra([FromBody] CrearCompraDto dto)
        {
            Result<CompraDto> resultado = await _comprasService.CrearCompraAsync(dto, ObtenerUsuarioActual());
            if (resultado.Success && resultado.Data != null)
            {
                return CreatedAtAction(nameof(ObtenerCompraPorId), new { id = resultado.Data.CompraId }, resultado.Data);
            }

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,EncargadoCompras,Almacen")]
        [HttpPut("{id:int}/Recibir")]
        public async Task<IActionResult> RecibirCompra(int id)
        {
            Result<CompraDto> resultado = await _comprasService.RecibirCompraAsync(id, ObtenerUsuarioActual());
            return HandleResult(resultado);
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,Gerente")]
        [HttpPut("{id:int}/Anular")]
        public async Task<IActionResult> AnularCompra(int id)
        {
            Result<CompraDto> resultado = await _comprasService.AnularCompraAsync(id, ObtenerUsuarioActual());
            return HandleResult(resultado);
        }

        [Authorize(Roles = "Administrador,SuperUsuario,Admin,EncargadoCompras,Contabilidad")]
        [HttpPost("Pago")]
        public async Task<IActionResult> RegistrarPago([FromBody] CrearPagoCompraDto dto)
        {
            Result<PagoCompraDto> resultado = await _comprasService.RegistrarPagoAsync(dto);

            if (resultado.Success)
                return StatusCode(201, resultado.Data);

            if (resultado.IsValidationError)
                return BadRequest(new { mensaje = resultado.Error });

            return Conflict(new { mensaje = resultado.Error });
        }
    }
}
