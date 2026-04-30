using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Perfiles;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class PerfilesService : IPerfilesService
    {
        private readonly WebWaveDbContext _context;

        public PerfilesService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<PerfilesDto>>> ObtenerPerfilesAsync()
        {
            var perfiles = await _context.Perfiles
                .AsNoTracking()
                .Include(p => p.Permisos)
                    .ThenInclude(pp => pp.Permiso)
                .ToListAsync();

            var resultado = perfiles.Select(p => new PerfilesDto
            {
                PerfilId = p.PerfilId,
                Nombre = p.Nombre,
                Permisos = p.Permisos
                    .Select(pp => pp.Permiso.Nombre)
                    .ToList()
            });

            return Result<IEnumerable<PerfilesDto>>.Ok(resultado);
        }

        public async Task<Result<PerfilesDto>> ObtenerPerfilPorIdAsync(int perfilId)
        {
            var perfil = await _context.Perfiles
                .AsNoTracking()
                .Include(p => p.Permisos)
                    .ThenInclude(pp => pp.Permiso)
                .FirstOrDefaultAsync(p => p.PerfilId == perfilId);

            if (perfil is null)
                return Result<PerfilesDto>.Fail($"Perfil con Id {perfilId} no encontrado.");

            return Result<PerfilesDto>.Ok(new PerfilesDto
            {
                PerfilId = perfil.PerfilId,
                Nombre = perfil.Nombre,
                Permisos = perfil.Permisos
                    .Select(pp => pp.Permiso.Nombre)
                    .ToList()
            });
        }

        public async Task<Result<PerfilesDto>> CrearPerfilAsync(PerfilesCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PerfilesDto>.Fail("El nombre del perfil es obligatorio.", isValidation: true);

            var existe = await _context.Perfiles
                .AnyAsync(p => p.Nombre == dto.Nombre);

            if (existe)
                return Result<PerfilesDto>.Fail(
                    $"Ya existe un perfil con el nombre '{dto.Nombre}'.");

            if (dto.PermisosIds.Count != 0)
            {
                var permisosExistentes = await _context.Permisos
                    .Where(p => dto.PermisosIds.Contains(p.PermisoId))
                    .Select(p => p.PermisoId)
                    .ToListAsync();

                var noEncontrados = dto.PermisosIds
                    .Except(permisosExistentes)
                    .ToList();

                if (noEncontrados.Count != 0)
                    return Result<PerfilesDto>.Fail($"Los siguientes permisos no existen: " + $"{string.Join(", ", noEncontrados)}.");
            }

            var perfil = new Perfiles { Nombre = dto.Nombre.Trim() };
            _context.Perfiles.Add(perfil);
            await _context.SaveChangesAsync();

            if (dto.PermisosIds.Count != 0)
            {
                var perfilesPermisos = dto.PermisosIds
                    .Select(pid => new PerfilesPermisos
                    {
                        PerfilId = perfil.PerfilId,
                        PermisoId = pid
                    }).ToList();

                _context.PerfilesPermisos.AddRange(perfilesPermisos);
                await _context.SaveChangesAsync();
            }

            return await ObtenerPerfilPorIdAsync(perfil.PerfilId);
        }

        public async Task<Result<PerfilesDto>> ActualizarPerfilAsync(int perfilId, PerfilesActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PerfilesDto>.Fail("El nombre del perfil es obligatorio.", isValidation: true);

            var perfil = await _context.Perfiles
                .Include(p => p.Permisos)
                .FirstOrDefaultAsync(p => p.PerfilId == perfilId);

            if (perfil is null)
                return Result<PerfilesDto>.Fail($"El perfil con Id {perfilId} no existe.");

            var nombreDuplicado = await _context.Perfiles
                .AnyAsync(p => p.Nombre == dto.Nombre && p.PerfilId != perfilId);

            if (nombreDuplicado)
                return Result<PerfilesDto>.Fail($"Ya existe otro perfil con el nombre '{dto.Nombre}'.");

            if (dto.PermisosIds.Count != 0)
            {
                var permisosExistentes = await _context.Permisos
                    .Where(p => dto.PermisosIds.Contains(p.PermisoId))
                    .Select(p => p.PermisoId)
                    .ToListAsync();

                var noEncontrados = dto.PermisosIds
                    .Except(permisosExistentes)
                    .ToList();

                if (noEncontrados.Count != 0)
                    return Result<PerfilesDto>.Fail($"Los siguientes permisos no existen: " + $"{string.Join(", ", noEncontrados)}.");
            }

            perfil.Nombre = dto.Nombre.Trim();

            var permisosActuales = perfil.Permisos
                .Select(p => p.PermisoId)
                .ToHashSet();

            var permisosNuevos = dto.PermisosIds.ToHashSet();

            var aEliminar = perfil.Permisos
                .Where(p => !permisosNuevos.Contains(p.PermisoId))
                .ToList();

            var aAgregar = permisosNuevos
                .Except(permisosActuales)
                .Select(pid => new PerfilesPermisos
                {
                    PerfilId = perfilId,
                    PermisoId = pid
                }).ToList();

            if (aEliminar.Count != 0)
                _context.PerfilesPermisos.RemoveRange(aEliminar);

            if (aAgregar.Count != 0)
                _context.PerfilesPermisos.AddRange(aAgregar);

            await _context.SaveChangesAsync();

            return await ObtenerPerfilPorIdAsync(perfilId);
        }

        public async Task<Result> EliminarPerfilAsync(int perfilId)
        {
            var perfil = await _context.Perfiles
                .Include(p => p.Permisos)
                .Include(p => p.Usuarios)
                .FirstOrDefaultAsync(p => p.PerfilId == perfilId);

            if (perfil is null)
                return Result.Fail($"El perfil con Id {perfilId} no existe.");

            if (perfil.Usuarios.Count != 0)
                return Result.Fail($"No se puede eliminar el perfil '{perfil.Nombre}' " +
                    $"porque tiene {perfil.Usuarios.Count} usuario(s) asignado(s).");

            _context.PerfilesPermisos.RemoveRange(perfil.Permisos);
            perfil.Activo = false;
            _context.Perfiles.Update(perfil);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
