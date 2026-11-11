using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Auth;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class AuthService : IAuthService
    {
        private readonly WebWaveDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditoriaService? _auditoriaService;

        public AuthService(
            WebWaveDbContext context,
            IJwtTokenService jwtTokenService,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor,
            IAuditoriaService? auditoriaService = null)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _auditoriaService = auditoriaService;
        }

        public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Desconocida";

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                _logger.LogWarning("Intento de login con email vacío desde IP: {Ip}", ip);
                return Result<LoginResponseDto>.Fail("El email es obligatorio.", isValidation: true);
            }

            if (string.IsNullOrWhiteSpace(dto.Contraseña))
            {
                _logger.LogWarning("Intento de login con contraseña vacía para {Email} desde IP: {Ip}", dto.Email, ip);
                return Result<LoginResponseDto>.Fail("La contraseña es obligatoria.", isValidation: true);
            }

            string email = dto.Email.Trim();
            string emailLower = email.ToLower();

            Usuarios? usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Email == email || u.Email == emailLower);

            if (usuario is null || !BCryptNet.Verify(dto.Contraseña, usuario.Contraseña))
            {
                _logger.LogWarning("FALLO DE AUTENTICACIÓN: Credenciales inválidas para el correo '{Email}' desde IP: {Ip}", email, ip);
                return Result<LoginResponseDto>.Fail("Credenciales incorrectas.", isValidation: true);
            }

            string nombrePerfil = usuario.Perfil?.Nombre ?? "Administrador";

            int ingresosPrevios = 0;
            int totalIngresos = 1;

            if (_auditoriaService != null)
            {
                try
                {
                    ingresosPrevios = await _context.Auditoria
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .CountAsync(a => a.Tipo == "USUARIOS"
                                      && a.EntidadId == usuario.UsuarioId
                                      && a.Accion == "INGRESO AL SISTEMA");

                    totalIngresos = ingresosPrevios + 1;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar el historial de ingresos para UsuarioId {UsuarioId}.", usuario.UsuarioId);
                }

                string usuarioAudit = string.IsNullOrWhiteSpace(usuario.Nombre) ? usuario.Email : usuario.Nombre.Trim();
                if (usuarioAudit.Length > 100) usuarioAudit = usuarioAudit[..100];

                string valorAnterior = ingresosPrevios == 1 ? "1 ingreso" : $"{ingresosPrevios} ingresos";
                string valorNuevo = totalIngresos == 1 ? "1 ingreso" : $"{totalIngresos} ingresos";

                await _auditoriaService.RegistrarEventoAsync(
                    "USUARIOS",
                    usuario.UsuarioId,
                    usuario.Email,
                    usuarioAudit,
                    "INGRESO AL SISTEMA",
                    $"El usuario '{usuario.Nombre}' ({usuario.Email}) con perfil '{nombrePerfil}' ingresó al sistema (Ingreso #{totalIngresos}). IP: {ip}.",
                    valorAnterior,
                    valorNuevo,
                    "Inicio de sesión exitoso"
                );
            }

            _logger.LogInformation("LOGIN EXITOSO: Usuario {Email} (ID: {UsuarioId}, Perfil: {Perfil}, Ingreso #{TotalIngresos}) autenticado desde IP: {Ip}",
                usuario.Email, usuario.UsuarioId, nombrePerfil, totalIngresos, ip);

            LoginTokenResult tokenResult = _jwtTokenService.GenerarToken(
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Email,
                usuario.PerfilId,
                nombrePerfil);

            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Token = tokenResult.Token,
                Expira = tokenResult.Expira,
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Perfil = nombrePerfil
            });
        }
    }
}
