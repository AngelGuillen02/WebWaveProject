namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public bool TieneNumeroSerie { get; set; } = false;
        public bool TieneLote { get; set; } = false;
        public string? CodigoBarras { get; set; }
        public string TipoISV { get; set; } = "ISV15";
        public ICollection<Inventarios> Inventarios { get; set; } = [];
        public bool Activo { get; set; } = true;
    }
}
