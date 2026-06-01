using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Caja.Dtos;

namespace SistemaVisionTech.Features.Caja.Interfaces
{
    public interface ICajaService
    {
        Task<Result<CierreCajaResponseDto>> AbrirCajaAsync(AbrirCajaDto dto, int usuarioId);
        Task<Result<CierreCajaResponseDto>> CerrarCajaAsync(int cajaId, CerrarCajaDto dto);
        Task<Result<EstadoCajaResponseDto>> ObtenerEstadoActualAsync(int sucursalId);
        Task<Result<List<CierreCajaResponseDto>>> ObtenerHistorialAsync(int sucursalId);
    }
}
