using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface ISucursalesService
    {
        Task<Result<IEnumerable<SucursalesDto>>> ObtenerSucursalesAsync();
        Task<Result<SucursalesDto>> ObtenerSucursalPorIdAsync(int sucursalId);
        Task<Result<SucursalesDto>> CrearSucursalAsync(SucursalesCreacionDto dto);
        Task<Result<SucursalesDto>> ActualizarSucursalAsync(int sucursalId, SucursalesActualizacionDto dto);
        Task<Result> EliminarSucursalAsync(int sucursalId);
    }
}
