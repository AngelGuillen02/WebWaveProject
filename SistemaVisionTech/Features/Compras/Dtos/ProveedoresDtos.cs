namespace SistemaVisionTech.Features.Compras.Dtos
{
    public class ProveedorCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Contacto { get; set; }
        public string? RTN { get; set; }
    }

    public class ProveedorResponseDto
    {
        public int ProveedorId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Contacto { get; set; }
        public string? RTN { get; set; }
        public bool Activo { get; set; }
    }
}
