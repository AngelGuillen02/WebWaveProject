using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Trazabilidad.Dtos;

namespace SistemaVisionTech.Features.Trazabilidad.Interfaces
{
    public interface ISeriesProductoService
    {
        Task<Result<List<SerieProductoResponseDto>>> ListarAsync();
        Task<Result<SerieProductoResponseDto>> ObtenerPorIdAsync(int id);
        Task<Result<SerieProductoResponseDto>> CrearAsync(SerieProductoCreacionDto dto);
        Task<Result<bool>> EliminarAsync(int id);
    }

    public interface ILotesProductoService
    {
        Task<Result<List<LoteProductoResponseDto>>> ListarAsync();
        Task<Result<LoteProductoResponseDto>> ObtenerPorIdAsync(int id);
        Task<Result<LoteProductoResponseDto>> CrearAsync(LoteProductoCreacionDto dto);
        Task<Result<LoteProductoResponseDto>> EditarAsync(int id, LoteProductoCreacionDto dto);
        Task<Result<bool>> EliminarAsync(int id);
    }
}
