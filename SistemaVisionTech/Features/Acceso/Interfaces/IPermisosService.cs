using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Permisos;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IPermisosService
    {
        Task<Result<IEnumerable<PermisosDto>>> ObtenerPermisosAsync();
        Task<Result<PermisosDto>> CrearPermisoAsync(PermisosCreacionDto dto);
        Task<Result<PermisosDto>> ActualizarPermisoAsync(int permisoId, PermisosActualizacionDto dto);
        Task<Result> EliminarPermisoAsync(int permisoId);
    }
}
