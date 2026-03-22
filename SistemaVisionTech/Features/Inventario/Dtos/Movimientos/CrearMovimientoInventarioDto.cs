using SistemaVisionTech.Features.Inventario.Enums;

namespace SistemaVisionTech.Features.Inventario.Dtos
{
    public class CrearMovimientoInventarioDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public TipoMovimientoEnum TipoMovimiento { get; set; }
    }
}
