namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Productos
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public ICollection<Inventario> Inventarios { get; set; } = [];
        public bool Activo { get; set; } = true;
    }
}
