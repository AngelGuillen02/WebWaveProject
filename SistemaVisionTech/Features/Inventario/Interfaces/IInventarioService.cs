using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Inventario.Dtos;
using SistemaVisionTech.Features.Inventario.Dtos.Inventario;
using SistemaVisionTech.Features.Inventario.Dtos.Movimientos;

namespace SistemaVisionTech.Features.Inventario.Interfaces
{
    public interface IInventarioService
    {
        Task<Result<IEnumerable<InventarioDto>>> ObtenerInventarioAsync();
        Task<Result<InventarioDto>> ObtenerInventarioPorProductoAsync(int productoId);
        Task<Result<MovimientoDto>> RegistrarMovimientoAsync(CrearMovimientoInventarioDto dto);
        Task<Result<IEnumerable<MovimientoDto>>> ObtenerMovimientosAsync(int? productoId = null);
        Task<Result<InventarioDto>> AjustarInventarioAsync(AjusteInventarioDto dto);
    }
}
