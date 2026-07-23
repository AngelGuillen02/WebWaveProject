using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Navegacion.Dtos;

namespace SistemaVisionTech.Features.Navegacion.Interfaces
{
    public interface INavegacionService
    {
        Task<Result<IEnumerable<NavNodoDto>>> ObtenerArbolAsync();
        Task<Result<NavNodoDto>> CrearNodoAsync(NavNodoCreacionDto dto);
        Task<Result<NavNodoDto>> ActualizarNodoAsync(int nodoId, NavNodoCreacionDto dto);
        Task<Result> EliminarNodoAsync(int nodoId);
        Task<Result<NavNodoDto>> GuardarPerfilesDelNodoAsync(int nodoId, GuardarPerfilesNodoDto dto);
        Task<Result<IEnumerable<MenuNodoDto>>> ObtenerMenuAsync(int perfilId);
        Task<Result<IEnumerable<NavNodoConAccesoDto>>> ObtenerArbolConAccesoAsync(int perfilId);
        Task<Result> GuardarRestriccionesRolAsync(int perfilId, SetRestriccionesRolDto dto);
    }
}
