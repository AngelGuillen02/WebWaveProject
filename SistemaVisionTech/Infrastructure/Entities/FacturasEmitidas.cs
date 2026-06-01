namespace SistemaVisionTech.Infrastructure.Entities
{
    public class FacturasEmitidas
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int SucursalId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string CAI { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
        public string? RTNCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal MontoExento { get; set; } = 0;
        public decimal MontoGravado15 { get; set; } = 0;
        public decimal MontoGravado18 { get; set; } = 0;
        public decimal ISV15 { get; set; } = 0;
        public decimal ISV18 { get; set; } = 0;
        public decimal Total { get; set; }
        public bool Activo { get; set; } = true;

        // Navegaciones
        public Venta? Venta { get; set; }
        public Sucursales? Sucursal { get; set; }
    }
}
