using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Perfiles;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IPerfilesService
    {
        Task<Result<IEnumerable<PerfilesDto>>> ObtenerPerfilesAsync();
        Task<Result<PerfilesDto>> ObtenerPerfilPorIdAsync(int perfilId);
        Task<Result<PerfilesDto>> CrearPerfilAsync(PerfilesCreacionDto dto);
        Task<Result<PerfilesDto>> ActualizarPerfilAsync(int perfilId, PerfilesActualizacionDto dto);
        Task<Result> EliminarPerfilAsync(int perfilId);
    }
}
