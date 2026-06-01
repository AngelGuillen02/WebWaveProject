using SistemaVisionTech.Common;
using SistemaVisionTech.Features.SAR.Dtos;

namespace SistemaVisionTech.Features.SAR.Interfaces
{
    public interface ISARService
    {
        Task<Result<ConfiguracionSARDto>> CrearConfiguracionAsync(CrearConfiguracionSARDto dto);
        Task<Result<ConfiguracionSARDto>> ObtenerConfiguracionActualAsync(int sucursalId);
        Task<Result<FacturaEmitidaResponseDto>> EmitirFacturaAsync(int ventaId, int sucursalId);
        Task<Result<FacturaEmitidaResponseDto>> ObtenerFacturaAsync(int ventaId);
    }
}
