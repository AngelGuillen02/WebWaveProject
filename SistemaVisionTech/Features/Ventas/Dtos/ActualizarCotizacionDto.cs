namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class ActualizarCotizacionDto
    {
        public int ClienteId { get; set; }
        public string? Motivo { get; set; }
        public List<CrearVentaDetalleDto> Detalles { get; set; } = [];
    }
}
