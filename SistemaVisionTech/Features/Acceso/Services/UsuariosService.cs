using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using Microsoft.Extensions.Logging;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class UsuariosService : IUsuariosService
    {
        private readonly WebWaveDbContext _context;
        private readonly ILogger<UsuariosService> _logger;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IPermisosCacheService _permisosCache;

        public UsuariosService(WebWaveDbContext context, ILogger<UsuariosService> logger, IAuditoriaService auditoriaService, IPermisosCacheService permisosCache)
        {
            _context = context;
            _logger = logger;
            _auditoriaService = auditoriaService;
            _permisosCache = permisosCache;
        }

        public async Task<Result<IEnumerable<UsuariosDto>>> ObtenerUsuariosAsync(bool esAdmin = false)
        {
            var usuariosEntities = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Activo)
                .Include(u => u.Perfil)
                .ToListAsync();

            var usuarios = usuariosEntities.Select(u => new UsuariosDto
            {
                UsuarioId = u.UsuarioId,
                Nombre = u.Nombre,
                Email = u.Email,
                PerfilId = u.PerfilId,
                Perfil = u.Perfil != null ? u.Perfil.Nombre : "Sin Perfil"
            }).ToList();

            return Result<IEnumerable<UsuariosDto>>.Ok(usuarios);
        }

        public async Task<Result<UsuariosDto>> ObtenerUsuarioPorIdAsync(int usuarioId, bool esAdmin = false)
        {
            Usuarios? usuario = await _context.Usuarios
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
                Perfil = usuario.Perfil != null ? usuario.Perfil.Nombre : "Sin Perfil"
            });
        }

        public async Task<Result<UsuariosDto>> CrearUsuarioAsync(UsuariosCreacionDto dto, string? usuarioActual = null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<UsuariosDto>.Fail("El nombre del usuario es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<UsuariosDto>.Fail("El email es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Contraseña))
                return Result<UsuariosDto>.Fail("La contraseña es obligatoria.", isValidation: true);

            if (dto.Contraseña.Length < 8)
                return Result<UsuariosDto>.Fail("La contraseña debe tener al menos 8 caracteres.", isValidation: true);

            if (!dto.Contraseña.Any(char.IsDigit))
                return Result<UsuariosDto>.Fail("La contraseña debe contener al menos un número.", isValidation: true);

            string emailNormalizado = dto.Email.Trim().ToLower();

            bool perfilExiste = await _context.Perfiles
                .AnyAsync(p => p.PerfilId == dto.PerfilId);

            if (!perfilExiste)
                return Result<UsuariosDto>.Fail($"El perfil con Id {dto.PerfilId} no existe.", isValidation: true);

            Usuarios? usuarioExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuarioExistente != null)
            {
                if (usuarioExistente.Activo)
                {
                    return Result<UsuariosDto>.Fail($"Ya existe un usuario activo registrado con el correo '{dto.Email}'.", isValidation: true);
                }

                // Si el usuario existía pero estaba eliminado (inactivo), se reactiva automáticamente con los nuevos datos
                usuarioExistente.Nombre = dto.Nombre.Trim();
                usuarioExistente.Contraseña = BCryptNet.HashPassword(dto.Contraseña.Trim());
                _permisosCache.InvalidarUsuario(usuarioExistente.UsuarioId);
                usuarioExistente.PerfilId = dto.PerfilId;
                usuarioExistente.Activo = true;

                _context.Usuarios.Update(usuarioExistente);
                await _context.SaveChangesAsync();
                _logger.LogInformation("AUDITORÍA DE ACCESO: Usuario reactivado exitosamente con nuevos datos: {Email} (ID: {UsuarioId})", usuarioExistente.Email, usuarioExistente.UsuarioId);

                await _auditoriaService.RegistrarEventoAsync(
                    "USUARIOS",
                    usuarioExistente.UsuarioId,
                    usuarioExistente.Email,
                    usuarioActual,
                    "REACTIVACIÓN USUARIO",
                    $"Usuario reactivado: '{usuarioExistente.Nombre}' ({usuarioExistente.Email}). Perfil ID: {usuarioExistente.PerfilId}.",
                    "Inactivo",
                    "Activo",
                    "Reactivación de usuario"
                );

                return await ObtenerUsuarioPorIdAsync(usuarioExistente.UsuarioId, true);
            }

            Usuarios usuario = new()
            {
                Nombre = dto.Nombre.Trim(),
                Email = emailNormalizado,
                Contraseña = BCryptNet.HashPassword(dto.Contraseña.Trim()),
                PerfilId = dto.PerfilId,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            _logger.LogInformation("AUDITORÍA DE ACCESO: Usuario creado exitosamente: {Email} (ID: {UsuarioId}, Perfil: {PerfilId})", usuario.Email, usuario.UsuarioId, usuario.PerfilId);

            await _auditoriaService.RegistrarEventoAsync(
                "USUARIOS",
                usuario.UsuarioId,
                usuario.Email,
                usuarioActual,
                "CREACIÓN USUARIO",
                $"Nuevo usuario registrado: '{usuario.Nombre}' ({usuario.Email}). Perfil ID: {usuario.PerfilId}.",
                "-",
                usuario.Email,
                "Creación de usuario en el sistema"
            );

            return await ObtenerUsuarioPorIdAsync(usuario.UsuarioId, true);
        }

        public async Task<Result<UsuariosDto>> ActualizarUsuarioAsync(int usuarioId, UsuariosActualizacionDto dto, bool esAdmin = false, string? usuarioActual = null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<UsuariosDto>.Fail("El nombre es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<UsuariosDto>.Fail("El email es obligatorio.", isValidation: true);

            Usuarios? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result<UsuariosDto>.Fail(
                    $"Usuario con Id {usuarioId} no encontrado.");

            string emailNormalizado = dto.Email.Trim().ToLower();

            bool emailDuplicado = await _context.Usuarios
                .AnyAsync(u => u.Email.ToLower() == emailNormalizado
                            && u.UsuarioId != usuarioId
                            && u.Activo);

            if (emailDuplicado)
                return Result<UsuariosDto>.Fail(
                    $"El correo '{dto.Email}' ya está registrado por otro usuario.", isValidation: true);

            var cambios = new List<string>();
            if (usuario.Nombre != dto.Nombre.Trim())
                cambios.Add($"Nombre: '{usuario.Nombre}' -> '{dto.Nombre.Trim()}'");
            if (usuario.Email != emailNormalizado)
                cambios.Add($"Email: '{usuario.Email}' -> '{emailNormalizado}'");

            // Solo administradores pueden alterar el rol o perfil asignado a un usuario
            if (esAdmin && dto.PerfilId > 0 && dto.PerfilId != usuario.PerfilId)
            {
                bool perfilExiste = await _context.Perfiles
                    .AnyAsync(p => p.PerfilId == dto.PerfilId);

                if (!perfilExiste)
                    return Result<UsuariosDto>.Fail($"El perfil con Id {dto.PerfilId} no existe.", isValidation: true);

                cambios.Add($"Perfil ID: {usuario.PerfilId} -> {dto.PerfilId}");
                usuario.PerfilId = dto.PerfilId;
            }

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Email = emailNormalizado;

            if (!string.IsNullOrWhiteSpace(dto.Contraseña))
            {
                if (dto.Contraseña.Length < 8)
                    return Result<UsuariosDto>.Fail("La contraseña debe tener al menos 8 caracteres.", isValidation: true);

                if (!dto.Contraseña.Any(char.IsDigit))
                    return Result<UsuariosDto>.Fail("La contraseña debe contener al menos un número.", isValidation: true);

                usuario.Contraseña = BCrypt.Net.BCrypt.HashPassword(dto.Contraseña.Trim());
                
                cambios.Add("Contraseña modificada");
            }

            _permisosCache.InvalidarUsuario(usuarioId);
            await _context.SaveChangesAsync();

            if (cambios.Count > 0)
            {
                await _auditoriaService.RegistrarEventoAsync(
                    "USUARIOS",
                    usuarioId,
                    usuario.Email,
                    usuarioActual,
                    "MODIFICACIÓN USUARIO",
                    string.Join(" | ", cambios),
                    "-",
                    usuario.Email,
                    "Actualización de datos"
                );
            }

            return await ObtenerUsuarioPorIdAsync(usuarioId, esAdmin);
        }

        public async Task<Result> EliminarUsuarioAsync(int usuarioId, string? usuarioActual = null)
        {
            Usuarios? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result.Fail($"El usuario con Id {usuarioId} no existe.");

            if (usuario.UsuarioId == 1)
                return Result.Fail("No se puede eliminar el usuario administrador principal.");

            usuario.Activo = false;
            _permisosCache.InvalidarUsuario(usuarioId);
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
            _logger.LogWarning("AUDITORÍA DE ACCESO: Usuario desactivado/eliminado lógicamente: {Email} (ID: {UsuarioId})", usuario.Email, usuario.UsuarioId);

            await _auditoriaService.RegistrarEventoAsync(
                "USUARIOS",
                usuarioId,
                usuario.Email,
                usuarioActual,
                "BAJA USUARIO",
                $"Usuario '{usuario.Nombre}' ({usuario.Email}) dado de baja lógicamente.",
                usuario.Email,
                "Inactivo (Eliminado)",
                "Baja de usuario del sistema"
            );

            return Result.Ok();
        }
    }
}
