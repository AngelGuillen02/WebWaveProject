namespace SistemaVisionTech.Infrastructure.Entities
{
    public class SeriesProducto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public int? CompraDetalleId { get; set; }
        public int? VentaDetalleId { get; set; }
        public string Estado { get; set; } = "Disponible"; // Disponible, Vendido
        public bool Activo { get; set; } = true;

        // Navegaciones
        public Producto? Producto { get; set; }
        public ComprasDetalles? CompraDetalle { get; set; }
        public VentasDetalles? VentaDetalle { get; set; }
    }
}
