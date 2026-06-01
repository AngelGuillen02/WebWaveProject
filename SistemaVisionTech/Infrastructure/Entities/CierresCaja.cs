namespace SistemaVisionTech.Infrastructure.Entities
{
    public class CierresCaja
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoApertura { get; set; } = 0;
        public decimal? MontoEfectivoFinal { get; set; }
        public decimal TotalVentasEfectivo { get; set; } = 0;
        public decimal TotalVentasTarjeta { get; set; } = 0;
        public decimal TotalVentasOtros { get; set; } = 0;
        public decimal? Diferencia { get; set; }
        public string Estado { get; set; } = "Abierto"; // Abierto, Cerrado
        public bool Activo { get; set; } = true;

        // Navegaciones
        public Sucursales? Sucursal { get; set; }
        public Usuarios? Usuario { get; set; }
    }
}
