namespace SistemaVisionTech.Infrastructure.Entities
{
    public class CotizacionesHistorial
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public string? NumeroCotizacion { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string Accion { get; set; } = string.Empty; // "CREACION", "MODIFICACION", "FACTURADA", "ANULADA"
        public string DetalleCambios { get; set; } = string.Empty;
        public decimal? TotalAnterior { get; set; }
        public decimal TotalNuevo { get; set; }
        public string? Motivo { get; set; }
        public bool Activo { get; set; } = true;

        public Venta? Venta { get; set; }
    }
}
