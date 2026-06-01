namespace SistemaVisionTech.Infrastructure.Entities
{
    public class LotesProducto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string NumeroLote { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        public int Cantidad { get; set; } = 0;
        public int? CompraDetalleId { get; set; }
        public bool Activo { get; set; } = true;

        // Navegaciones
        public Producto? Producto { get; set; }
        public ComprasDetalles? CompraDetalle { get; set; }
    }
}
