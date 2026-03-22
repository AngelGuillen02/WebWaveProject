namespace SistemaVisionTech.Features.Compras.Dtos.Compras
{
    public class CrearCompraDto
    {
        public int ProveedorId { get; set; }
        public List<CrearCompraDetalleDto> Detalles { get; set; } = [];
    }
}
