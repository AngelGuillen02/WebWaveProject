using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Productos.Dtos;

namespace SistemaVisionTech.Features.Productos.Interfaces
{
    public interface IProductosService
    {
        Task<Result<List<ProductoResponseDto>>> ListarAsync();
        Task<Result<ProductoResponseDto>> ObtenerPorIdAsync(int id);
        Task<Result<ProductoResponseDto>> CrearAsync(ProductoCreacionDto dto);
        Task<Result<ProductoResponseDto>> EditarAsync(int id, ProductoCreacionDto dto, string? usuario = null);
        Task<Result<List<SistemaVisionTech.Infrastructure.Entities.ProductosHistorial>>> ObtenerHistorialAsync(int productoId);
        Task<Result<bool>> EliminarAsync(int id);
    }
}
