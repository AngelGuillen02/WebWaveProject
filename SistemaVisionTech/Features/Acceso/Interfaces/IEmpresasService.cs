using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Empresas;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IEmpresasService
    {
        Task<Result<IEnumerable<EmpresasDto>>> ObtenerEmpresasAsync();
        Task<Result<EmpresasDto>> ObtenerEmpresaPorIdAsync(int empresaId);
        Task<Result<EmpresasDto>> CrearEmpresaAsync(EmpresasCreacionDto dto);
        Task<Result<EmpresasDto>> ActualizarEmpresaAsync(int empresaId, EmpresasActualizacionDto dto);
        Task<Result> EliminarEmpresaAsync(int empresaId);
    }
}
