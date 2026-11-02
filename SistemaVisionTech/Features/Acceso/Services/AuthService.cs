using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Auth;
using SistemaVisionTech.Features.Acceso.Interfaces;
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

        public AuthService(
            WebWaveDbContext context,
            IJwtTokenService jwtTokenService,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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

            _logger.LogInformation("LOGIN EXITOSO: Usuario {Email} (ID: {UsuarioId}, Perfil: {Perfil}) autenticado desde IP: {Ip}",
                usuario.Email, usuario.UsuarioId, usuario.Perfil?.Nombre ?? "Sin Perfil", ip);

            string nombrePerfil = usuario.Perfil?.Nombre ?? "Administrador";

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
