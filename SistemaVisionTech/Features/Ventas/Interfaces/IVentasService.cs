using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Ventas.Dtos;

namespace SistemaVisionTech.Features.Ventas.Interfaces
{
    public interface IVentasService
    {
        Task<Result<IEnumerable<VentasDto>>> ObtenerVentasAsync();
        Task<Result<VentasDto>> ObtenerVentaPorIdAsync(int ventaId);
        Task<Result<VentasDto>> CrearVentaAsync(CrearVentaDto dto);
        Task<Result<VentasDto>> ActualizarCotizacionAsync(int ventaId, ActualizarCotizacionDto dto, string usuarioNombre);
        Task<Result<IEnumerable<CotizacionHistorialDto>>> ObtenerHistorialCotizacionAsync(int ventaId);
        Task<Result<VentasDto>> FacturarCotizacionAsync(int ventaId, int sucursalId, int metodoPagoId, string usuarioNombre);
        Task<Result<VentasDto>> ConfirmarVentaAsync(int ventaId);
        Task<Result<VentasDto>> AnularVentaAsync(int ventaId);
        Task<Result<PagoVentaDto>> RegistrarPagoAsync(CrearPagoVentaDto dto);
    }
}
