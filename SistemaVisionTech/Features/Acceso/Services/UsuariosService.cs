using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class UsuariosService : IUsuariosService
    {
        private readonly WebWaveDbContext _context;

        public UsuariosService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<UsuariosDto>>> ObtenerUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfil)
                .Select(u => new UsuariosDto
                {
                    UsuarioId = u.UsuarioId,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    PerfilId = u.PerfilId,
                    Perfil = u.Perfil.Nombre
                })
                .ToListAsync();

            return Result<IEnumerable<UsuariosDto>>.Ok(usuarios);
        }

        public async Task<Result<UsuariosDto>> ObtenerUsuarioPorIdAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result<UsuariosDto>.Fail(
                    $"Usuario con Id {usuarioId} no encontrado.");

            return Result<UsuariosDto>.Ok(new UsuariosDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                PerfilId = usuario.PerfilId,
                Perfil = usuario.Perfil.Nombre
            });
        }

        public async Task<Result<UsuariosDto>> CrearUsuarioAsync(UsuariosCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<UsuariosDto>.Fail("El nombre del usuario es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<UsuariosDto>.Fail("El email es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Contraseña))
                return Result<UsuariosDto>.Fail("La contraseña es obligatoria.", isValidation: true);

            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExiste)
                return Result<UsuariosDto>.Fail($"Ya existe un usuario con el email '{dto.Email}'.");

            var perfilExiste = await _context.Perfiles
                .AnyAsync(p => p.PerfilId == dto.PerfilId);

            if (!perfilExiste)
                return Result<UsuariosDto>.Fail($"El perfil con Id {dto.PerfilId} no existe.");

            var usuario = new Usuarios
            {
                Nombre = dto.Nombre.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Contraseña = BCryptNet.HashPassword(dto.Contraseña),
                PerfilId = dto.PerfilId
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await ObtenerUsuarioPorIdAsync(usuario.UsuarioId);
            return resultado;
        }

        public async Task<Result<UsuariosDto>> ActualizarUsuarioAsync(int usuarioId, UsuariosActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<UsuariosDto>.Fail("El nombre es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<UsuariosDto>.Fail("El email es obligatorio.", isValidation: true);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result<UsuariosDto>.Fail(
                    $"El usuario con Id {usuarioId} no existe.");

            var emailDuplicado = await _context.Usuarios
                .AnyAsync(u => u.Email == dto.Email
                            && u.UsuarioId != usuarioId);

            if (emailDuplicado)
                return Result<UsuariosDto>.Fail(
                    $"Ya existe otro usuario con el email '{dto.Email}'.");

            var perfilExiste = await _context.Perfiles
                .AnyAsync(p => p.PerfilId == dto.PerfilId);

            if (!perfilExiste)
                return Result<UsuariosDto>.Fail($"El perfil con Id {dto.PerfilId} no existe.");

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Email = dto.Email.Trim().ToLower();
            usuario.PerfilId = dto.PerfilId;

            await _context.SaveChangesAsync();

            return await ObtenerUsuarioPorIdAsync(usuarioId);
        }

        public async Task<Result> EliminarUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result.Fail($"El usuario con Id {usuarioId} no existe.");

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
