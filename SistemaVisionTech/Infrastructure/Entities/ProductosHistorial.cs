namespace SistemaVisionTech.Infrastructure.Entities
{
    public class ProductosHistorial
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
        public string Accion { get; set; } = "MODIFICACION"; // "CREACION", "MODIFICACION", "DESACTIVACION"
        public string DetalleCambios { get; set; } = string.Empty;
        public decimal? PrecioAnterior { get; set; }
        public decimal PrecioNuevo { get; set; }
        public bool Activo { get; set; } = true;

        public Producto? Producto { get; set; }
    }
}
