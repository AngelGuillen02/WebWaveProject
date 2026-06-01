namespace SistemaVisionTech.Infrastructure.Entities
{
    public class EstadosVenta
    {
        public int EstadoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public ICollection<Venta> Ventas { get; set; } = [];
        public bool Activo { get; set; } = true;
    }
}
