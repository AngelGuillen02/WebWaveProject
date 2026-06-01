using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Clientes.Dtos;

namespace SistemaVisionTech.Features.Clientes.Interfaces
{
    public interface IClientesService
    {
        Task<Result<List<ClienteResponseDto>>> ListarAsync();
        Task<Result<ClienteResponseDto>> ObtenerPorIdAsync(int id);
        Task<Result<ClienteResponseDto>> CrearAsync(ClienteCreacionDto dto);
        Task<Result<ClienteResponseDto>> EditarAsync(int id, ClienteCreacionDto dto);
        Task<Result<bool>> EliminarAsync(int id);
    }
}
