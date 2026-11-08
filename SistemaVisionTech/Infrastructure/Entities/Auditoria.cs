namespace SistemaVisionTech.Infrastructure.Entities
{
    public class Auditoria
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty; // "PRODUCTO", "CLIENTE", "PROVEEDOR", "VENTA", "COMPRA", "CAJA", "USUARIOS", "KARDEX"
        public int EntidadId { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string Accion { get; set; } = string.Empty; // "CREACION", "MODIFICACION", "ELIMINACION", "IMPORTACION", etc.
        public string Detalle { get; set; } = string.Empty;
        public string? ValorAnterior { get; set; } = "-";
        public string? ValorNuevo { get; set; } = "-";
        public string? Motivo { get; set; }
        public bool Activo { get; set; } = true;
    }
}
