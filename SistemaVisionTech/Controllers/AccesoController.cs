using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Features.Acceso.Dtos.Auth;
using SistemaVisionTech.Features.Acceso.Dtos.Empresas;
using SistemaVisionTech.Features.Acceso.Dtos.Perfiles;
using SistemaVisionTech.Features.Acceso.Dtos.Permisos;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;
using SistemaVisionTech.Features.Acceso.Interfaces;

namespace SistemaVisionTech.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccesoController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IUsuariosService _usuariosService;
        private readonly IPerfilesService _perfilesService;
        private readonly IPermisosService _permisosService;
        private readonly IEmpresasService _empresasService;
        private readonly ISucursalesService _sucursalesService;

        public AccesoController(
            IAuthService authService,
            IUsuariosService usuariosService,
            IPerfilesService perfilesService,
            IPermisosService permisosService,
            IEmpresasService empresasService,
            ISucursalesService sucursalesService)
        {
            _authService = authService;
            _usuariosService = usuariosService;
            _perfilesService = perfilesService;
            _permisosService = permisosService;
            _empresasService = empresasService;
            _sucursalesService = sucursalesService;
        }



        // ── AUTH ─────────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var resultado = await _authService.LoginAsync(dto);

            if (!resultado.Success)
                return Unauthorized(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        // ── USUARIOS ──────────────────────────────────────────────────────

        [HttpGet("Usuarios")]
        public async Task<IActionResult> ObtenerUsuarios()
        {
            var resultado = await _usuariosService.ObtenerUsuariosAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Usuarios/{id:int}")]
        public async Task<IActionResult> ObtenerUsuarioPorId(int id)
        {
            var resultado = await _usuariosService.ObtenerUsuarioPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [AllowAnonymous]
        [HttpPost("Usuarios")]
        public async Task<IActionResult> CrearUsuario(
            [FromBody] UsuariosCreacionDto dto)
        {
            var resultado = await _usuariosService.CrearUsuarioAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerUsuarioPorId),
                data => new { id = data.UsuarioId });
        }

        [HttpPut("Usuarios/{id:int}")]
        public async Task<IActionResult> ActualizarUsuario(
            int id, [FromBody] UsuariosActualizacionDto dto)
        {
            var resultado = await _usuariosService.ActualizarUsuarioAsync(id, dto);
            return HandleResult(resultado);
        }

        [Authorize(Roles = "Administrador")]
        [HttpDelete("Usuarios/{id:int}")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var resultado = await _usuariosService.EliminarUsuarioAsync(id);
            return HandleResult(resultado);
        }

        // ── PERFILES ──────────────────────────────────────────────────────

        [HttpGet("Perfiles")]
        public async Task<IActionResult> ObtenerPerfiles()
        {
            var resultado = await _perfilesService.ObtenerPerfilesAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Perfiles/{id:int}")]
        public async Task<IActionResult> ObtenerPerfilPorId(int id)
        {
            var resultado = await _perfilesService.ObtenerPerfilPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [AllowAnonymous]
        [HttpPost("Perfiles")]
        public async Task<IActionResult> CrearPerfil(
            [FromBody] PerfilesCreacionDto dto)
        {
            var resultado = await _perfilesService.CrearPerfilAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerPerfilPorId),
                data => new { id = data.PerfilId });
        }

        [HttpPut("Perfiles/{id:int}")]
        public async Task<IActionResult> ActualizarPerfil(
            int id, [FromBody] PerfilesActualizacionDto dto)
        {
            var resultado = await _perfilesService.ActualizarPerfilAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Perfiles/{id:int}")]
        public async Task<IActionResult> EliminarPerfil(int id)
        {
            var resultado = await _perfilesService.EliminarPerfilAsync(id);
            return HandleResult(resultado);
        }

        // ── PERMISOS ──────────────────────────────────────────────────────

        [HttpGet("Permisos")]
        public async Task<IActionResult> ObtenerPermisos()
        {
            var resultado = await _permisosService.ObtenerPermisosAsync();
            return HandleResult(resultado);
        }

        [HttpPost("Permisos")]
        public async Task<IActionResult> CrearPermiso(
            [FromBody] PermisosCreacionDto dto)
        {
            var resultado = await _permisosService.CrearPermisoAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerPerfilPorId),
                data => new { id = data.PermisoId });
        }

        [HttpPut("Permisos/{id:int}")]
        public async Task<IActionResult> ActualizarPermiso(
            int id, [FromBody] PermisosActualizacionDto dto)
        {
            var resultado = await _permisosService.ActualizarPermisoAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Permisos/{id:int}")]
        public async Task<IActionResult> EliminarPermiso(int id)
        {
            var resultado = await _permisosService.EliminarPermisoAsync(id);
            return HandleResult(resultado);
        }

        // ── EMPRESAS ──────────────────────────────────────────────────────

        [HttpGet("Empresas")]
        public async Task<IActionResult> ObtenerEmpresas()
        {
            var resultado = await _empresasService.ObtenerEmpresasAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Empresas/{id:int}")]
        public async Task<IActionResult> ObtenerEmpresaPorId(int id)
        {
            var resultado = await _empresasService.ObtenerEmpresaPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Empresas")]
        public async Task<IActionResult> CrearEmpresa(
            [FromBody] EmpresasCreacionDto dto)
        {
            var resultado = await _empresasService.CrearEmpresaAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerEmpresaPorId),
                data => new { id = data.EmpresaId });
        }

        [HttpPut("Empresas/{id:int}")]
        public async Task<IActionResult> ActualizarEmpresa(
            int id, [FromBody] EmpresasActualizacionDto dto)
        {
            var resultado = await _empresasService.ActualizarEmpresaAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Empresas/{id:int}")]
        public async Task<IActionResult> EliminarEmpresa(int id)
        {
            var resultado = await _empresasService.EliminarEmpresaAsync(id);
            return HandleResult(resultado);
        }

        // ── SUCURSALES ────────────────────────────────────────────────────

        [HttpGet("Sucursales")]
        public async Task<IActionResult> ObtenerSucursales()
        {
            var resultado = await _sucursalesService.ObtenerSucursalesAsync();
            return HandleResult(resultado);
        }

        [HttpGet("Sucursales/{id:int}")]
        public async Task<IActionResult> ObtenerSucursalPorId(int id)
        {
            var resultado = await _sucursalesService.ObtenerSucursalPorIdAsync(id);

            if (!resultado.Success)
                return NotFound(new { mensaje = resultado.Error });

            return Ok(resultado.Data);
        }

        [HttpPost("Sucursales")]
        public async Task<IActionResult> CrearSucursal(
            [FromBody] SucursalesCreacionDto dto)
        {
            var resultado = await _sucursalesService.CrearSucursalAsync(dto);
            return HandleCreatedResult(resultado,
                nameof(ObtenerSucursalPorId),
                data => new { id = data.SucursalId });
        }

        [HttpPut("Sucursales/{id:int}")]
        public async Task<IActionResult> ActualizarSucursal(
            int id, [FromBody] SucursalesActualizacionDto dto)
        {
            var resultado = await _sucursalesService.ActualizarSucursalAsync(id, dto);
            return HandleResult(resultado);
        }

        [HttpDelete("Sucursales/{id:int}")]
        public async Task<IActionResult> EliminarSucursal(int id)
        {
            var resultado = await _sucursalesService.EliminarSucursalAsync(id);
            return HandleResult(resultado);
        }
    }
}