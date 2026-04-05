using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Auth;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;

using BCryptNet = BCrypt.Net.BCrypt;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class AuthService : IAuthService
    {
        private readonly WebWaveDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(WebWaveDbContext context, IJwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<LoginResponseDto>.Fail("El email es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Contraseña))
                return Result<LoginResponseDto>.Fail("La contraseña es obligatoria.", isValidation: true);

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(
                    u => u.Email == dto.Email.Trim().ToLower());

            if (usuario is null || !BCryptNet.Verify(dto.Contraseña, usuario.Contraseña))
                return Result<LoginResponseDto>.Fail(
                    "Credenciales incorrectas.", isValidation: true);

            var tokenResult = _jwtTokenService.GenerarToken(
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Email,
                usuario.Perfil.Nombre);

            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Token = tokenResult.Token,
                Expira = tokenResult.Expira,
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Perfil = usuario.Perfil.Nombre
            });
        }
    }
}
