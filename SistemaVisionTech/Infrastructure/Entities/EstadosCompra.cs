namespace SistemaVisionTech.Infrastructure.Entities
{
    public class EstadosCompra
    {
        public int EstadoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public ICollection<Compras> Compras { get; set; } = [];
    }
}
