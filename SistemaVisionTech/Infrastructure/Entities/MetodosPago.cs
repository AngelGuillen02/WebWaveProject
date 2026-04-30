namespace SistemaVisionTech.Infrastructure.Entities
{
    public class MetodosPago
    {
        public int MetodoPagoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
