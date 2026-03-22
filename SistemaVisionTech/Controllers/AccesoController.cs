using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Empresas;
using SistemaVisionTech.Features.Acceso.Dtos.Perfiles;
using SistemaVisionTech.Features.Acceso.Dtos.Permisos;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;
using SistemaVisionTech.Features.Acceso.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccesoController : ControllerBase
    {
        private readonly IAccesosService _accesosService;

        public AccesoController(IAccesosService accesosService)
        {
            _accesosService = accesosService;
        }

        // ── Helper para manejar Result<T> de forma uniforme ──────────────

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.Success)
                return Ok(result.Data);

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }

        private IActionResult HandleResult(Result result)
        {
            if (result.Success)
                return NoContent();

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }

        private IActionResult HandleCreatedResult<T>(
            Result<T> result, string actionName, Func<T, object> routeValues)
        {
            if (result.Success)
                return CreatedAtAction(actionName, routeValues(result.Data!), result.Data);

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }

        // ── USUARIOS ──────────────────────────────────────────────────────

        [HttpGet("Usuarios")]
        public async Task<IActionResult> ObtenerUsuarios()
        {
            var resultado = await _accesosService.ObtenerUsuariosAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Usuarios/{id:int}")]
        public async Task<IActionResult> ObtenerUsuarioPorId(int id)
        {
            var resultado = await _accesosService.ObtenerUsuarioPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Usuarios")]
        public async Task<IActionResult> CrearUsuario(
            [FromBody] UsuariosCreacionDto dto)
        {
            var resultado = await _accesosService.CrearUsuarioAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerUsuarioPorId),
                data => new { id = data.UsuarioId });
        }

        [HttpPut("Usuarios/{id:int}")]
        public async Task<IActionResult> ActualizarUsuario(
            int id, [FromBody] UsuariosActualizacionDto dto)
        {
            var resultado = await _accesosService.ActualizarUsuarioAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Usuarios/{id:int}")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var resultado = await _accesosService.EliminarUsuarioAsync(id);
            return HandleResult(resultado);
        }

        // ── PERFILES ──────────────────────────────────────────────────────

        [HttpGet("Perfiles")]
        public async Task<IActionResult> ObtenerPerfiles()
        {
            var resultado = await _accesosService.ObtenerPerfilesAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Perfiles/{id:int}")]
        public async Task<IActionResult> ObtenerPerfilPorId(int id)
        {
            var resultado = await _accesosService.ObtenerPerfilPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Perfiles")]
        public async Task<IActionResult> CrearPerfil(
            [FromBody] PerfilesCreacionDto dto)
        {
            var resultado = await _accesosService.CrearPerfilAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerPerfilPorId),
                data => new { id = data.PerfilId });
        }

        [HttpPut("Perfiles/{id:int}")]
        public async Task<IActionResult> ActualizarPerfil(
            int id, [FromBody] PerfilesActualizacionDto dto)
        {
            var resultado = await _accesosService.ActualizarPerfilAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Perfiles/{id:int}")]
        public async Task<IActionResult> EliminarPerfil(int id)
        {
            var resultado = await _accesosService.EliminarPerfilAsync(id);
            return HandleResult(resultado);
        }

        // ── PERMISOS ──────────────────────────────────────────────────────

        [HttpGet("Permisos")]
        public async Task<IActionResult> ObtenerPermisos()
        {
            var resultado = await _accesosService.ObtenerPermisosAsync();
            return HandleResult(resultado);
        }

        [HttpPost("Permisos")]
        public async Task<IActionResult> CrearPermiso(
            [FromBody] PermisosCreacionDto dto)
        {
            var resultado = await _accesosService.CrearPermisoAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerPerfilPorId),
                data => new { id = data.PermisoId });
        }

        [HttpPut("Permisos/{id:int}")]
        public async Task<IActionResult> ActualizarPermiso(
            int id, [FromBody] PermisosActualizacionDto dto)
        {
            var resultado = await _accesosService.ActualizarPermisoAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Permisos/{id:int}")]
        public async Task<IActionResult> EliminarPermiso(int id)
        {
            var resultado = await _accesosService.EliminarPermisoAsync(id);
            return HandleResult(resultado);
        }

        // ── EMPRESAS ──────────────────────────────────────────────────────

        [HttpGet("Empresas")]
        public async Task<IActionResult> ObtenerEmpresas()
        {
            var resultado = await _accesosService.ObtenerEmpresasAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Empresas/{id:int}")]
        public async Task<IActionResult> ObtenerEmpresaPorId(int id)
        {
            var resultado = await _accesosService.ObtenerEmpresaPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Empresas")]
        public async Task<IActionResult> CrearEmpresa(
            [FromBody] EmpresasCreacionDto dto)
        {
            var resultado = await _accesosService.CrearEmpresaAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerEmpresaPorId),
                data => new { id = data.EmpresaId });
        }

        [HttpPut("Empresas/{id:int}")]
        public async Task<IActionResult> ActualizarEmpresa(
            int id, [FromBody] EmpresasActualizacionDto dto)
        {
            var resultado = await _accesosService.ActualizarEmpresaAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Empresas/{id:int}")]
        public async Task<IActionResult> EliminarEmpresa(int id)
        {
            var resultado = await _accesosService.EliminarEmpresaAsync(id);
            return HandleResult(resultado);
        }

        // ── SUCURSALES ────────────────────────────────────────────────────

        [HttpGet("Sucursales")]
        public async Task<IActionResult> ObtenerSucursales()
        {
            var resultado = await _accesosService.ObtenerSucursalesAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Sucursales/{id:int}")]
        public async Task<IActionResult> ObtenerSucursalPorId(int id)
        {
            var resultado = await _accesosService.ObtenerSucursalPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Sucursales")]
        public async Task<IActionResult> CrearSucursal(
            [FromBody] SucursalesCreacionDto dto)
        {
            var resultado = await _accesosService.CrearSucursalAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerSucursalPorId),
                data => new { id = data.SucursalId });
        }

        [HttpPut("Sucursales/{id:int}")]
        public async Task<IActionResult> ActualizarSucursal(
            int id, [FromBody] SucursalesActualizacionDto dto)
        {
            var resultado = await _accesosService.ActualizarSucursalAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Sucursales/{id:int}")]
        public async Task<IActionResult> EliminarSucursal(int id)
        {
            var resultado = await _accesosService.EliminarSucursalAsync(id);
            return HandleResult(resultado);
        }
    }
}