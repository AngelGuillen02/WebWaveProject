namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class CrearVentaDto
    {
        public int ClienteId { get; set; }
        public List<CrearVentaDetalleDto> Detalles { get; set; } = [];
    }
}
