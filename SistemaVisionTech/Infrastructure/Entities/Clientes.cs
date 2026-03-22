namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Clientes
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ICollection<Ventas> Ventas { get; set; } = [];
    }
}
