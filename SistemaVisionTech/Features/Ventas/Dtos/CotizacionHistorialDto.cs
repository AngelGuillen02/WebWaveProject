namespace SistemaVisionTech.Features.Ventas.Dtos
{
    public class CotizacionHistorialDto
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public string? NumeroCotizacion { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaModificacion { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string DetalleCambios { get; set; } = string.Empty;
        public decimal? TotalAnterior { get; set; }
        public decimal TotalNuevo { get; set; }
        public string? Motivo { get; set; }
    }
}
