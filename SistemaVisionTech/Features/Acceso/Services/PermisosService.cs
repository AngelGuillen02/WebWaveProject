using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Permisos;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class PermisosService : IPermisosService
    {
        private readonly WebWaveDbContext _context;

        public PermisosService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<PermisosDto>>> ObtenerPermisosAsync()
        {
            var permisos = await _context.Permisos
                .AsNoTracking()
                .Select(p => new PermisosDto
                {
                    PermisoId = p.PermisoId,
                    Nombre = p.Nombre
                })
                .ToListAsync();

            return Result<IEnumerable<PermisosDto>>.Ok(permisos);
        }

        public async Task<Result<PermisosDto>> CrearPermisoAsync(PermisosCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PermisosDto>.Fail("El nombre del permiso es obligatorio.", isValidation: true);

            var existe = await _context.Permisos
                .AnyAsync(p => p.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase));

            if (existe)
                return Result<PermisosDto>.Fail($"Ya existe un permiso con el nombre '{dto.Nombre}'.");

            var permiso = new Permisos { Nombre = dto.Nombre.Trim() };
            _context.Permisos.Add(permiso);
            await _context.SaveChangesAsync();

            return Result<PermisosDto>.Ok(new PermisosDto
            {
                PermisoId = permiso.PermisoId,
                Nombre = permiso.Nombre
            });
        }

        public async Task<Result<PermisosDto>> ActualizarPermisoAsync(int permisoId, PermisosActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PermisosDto>.Fail("El nombre del permiso es obligatorio.", isValidation: true);

            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => p.PermisoId == permisoId);

            if (permiso is null)
                return Result<PermisosDto>.Fail($"El permiso con Id {permisoId} no existe.");

            var nombreDuplicado = await _context.Permisos
                .AnyAsync(p => p.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase) && p.PermisoId != permisoId);

            if (nombreDuplicado)
                return Result<PermisosDto>.Fail($"Ya existe otro permiso con el nombre '{dto.Nombre}'.");

            permiso.Nombre = dto.Nombre.Trim();
            await _context.SaveChangesAsync();

            return Result<PermisosDto>.Ok(new PermisosDto
            {
                PermisoId = permiso.PermisoId,
                Nombre = permiso.Nombre
            });
        }

        public async Task<Result> EliminarPermisoAsync(int permisoId)
        {
            var permiso = await _context.Permisos
                .Include(p => p.Perfiles)
                .FirstOrDefaultAsync(p => p.PermisoId == permisoId);

            if (permiso is null)
                return Result.Fail($"El permiso con Id {permisoId} no existe.");

            if (permiso.Perfiles.Count != 0)
                return Result.Fail(
                    $"No se puede eliminar el permiso '{permiso.Nombre}' " +
                    $"porque está asignado a {permiso.Perfiles.Count} perfil(es).");

            _context.Permisos.Remove(permiso);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
