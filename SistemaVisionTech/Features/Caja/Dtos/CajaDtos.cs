namespace SistemaVisionTech.Features.Caja.Dtos
{
    public class AbrirCajaDto
    {
        public int SucursalId { get; set; }
        public decimal MontoApertura { get; set; }
    }

    public class CerrarCajaDto
    {
        public decimal MontoEfectivoFinal { get; set; }
    }

    public class CierreCajaResponseDto
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoApertura { get; set; }
        public decimal? MontoEfectivoFinal { get; set; }
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasTarjeta { get; set; }
        public decimal TotalVentasOtros { get; set; }
        public decimal? Diferencia { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class EstadoCajaResponseDto
    {
        public int Id { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
        public decimal MontoApertura { get; set; }
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasTarjeta { get; set; }
        public decimal TotalVentasOtros { get; set; }
    }
}
