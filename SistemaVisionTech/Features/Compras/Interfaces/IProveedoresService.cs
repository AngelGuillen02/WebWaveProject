using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos;

namespace SistemaVisionTech.Features.Compras.Interfaces
{
    public interface IProveedoresService
    {
        Task<Result<List<ProveedorResponseDto>>> ListarAsync();
        Task<Result<ProveedorResponseDto>> ObtenerPorIdAsync(int id);
        Task<Result<ProveedorResponseDto>> CrearAsync(ProveedorCreacionDto dto);
        Task<Result<ProveedorResponseDto>> EditarAsync(int id, ProveedorCreacionDto dto);
        Task<Result<bool>> EliminarAsync(int id);
    }
}
