using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Clientes.Dtos;

namespace SistemaVisionTech.Features.Clientes.Interfaces
{
    public interface IClientesService
    {
        Task<Result<List<ClienteResponseDto>>> ListarAsync(int? page = null, int? pageSize = null);
        Task<Result<ClienteResponseDto>> ObtenerPorIdAsync(int id);
        Task<Result<ClienteResponseDto>> CrearAsync(ClienteCreacionDto dto, string? usuario = null);
        Task<Result<ClienteResponseDto>> EditarAsync(int id, ClienteCreacionDto dto, string? usuario = null);
        Task<Result<ImportacionLoteResultadoDto>> ImportarLoteAsync(List<ClienteCreacionDto> dtos, string? usuario = null);
        Task<Result<bool>> EliminarAsync(int id, string? usuario = null);
    }
}
