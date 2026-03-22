using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Dtos.Compras;
using SistemaVisionTech.Features.Compras.Dtos.Pagos;

namespace SistemaVisionTech.Features.Compras.Interfeces
{
    public interface IComprasService
    {
        Task<Result<IEnumerable<CompraDto>>> ObtenerComprasAsync();
        Task<Result<CompraDto>> ObtenerCompraPorIdAsync(int compraId);
        Task<Result<CompraDto>> CrearCompraAsync(CrearCompraDto dto);
        Task<Result<CompraDto>> RecibirCompraAsync(int compraId);
        Task<Result<CompraDto>> AnularCompraAsync(int compraId);
        Task<Result<PagoCompraDto>> RegistrarPagoAsync(CrearPagoCompraDto dto);
    }
}
