using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos.Compras;
using SistemaVisionTech.Features.Compras.Dtos.Pagos;

namespace SistemaVisionTech.Features.Compras.Interfaces
{
    public interface IComprasService
    {
        Task<Result<IEnumerable<CompraDto>>> ObtenerComprasAsync();
        Task<Result<CompraDto>> ObtenerCompraPorIdAsync(int compraId);
        Task<Result<CompraDto>> CrearCompraAsync(CrearCompraDto dto, string? usuario = null);
        Task<Result<CompraDto>> RecibirCompraAsync(int compraId, string? usuario = null);
        Task<Result<CompraDto>> AnularCompraAsync(int compraId, string? usuario = null);
        Task<Result<PagoCompraDto>> RegistrarPagoAsync(CrearPagoCompraDto dto);
    }
}
