namespace SistemaVisionTech.Features.Clientes.Dtos
{
    public class ClienteCreacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TipoCliente { get; set; } = "Natural";
        public string? RTN { get; set; }
    }

    public class ClienteResponseDto
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TipoCliente { get; set; } = string.Empty;
        public string? RTN { get; set; }
        public bool Activo { get; set; }
    }
}
