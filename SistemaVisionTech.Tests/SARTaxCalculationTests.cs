using Xunit;

namespace SistemaVisionTech.Tests
{
    public class SARTaxCalculationTests
    {
        [Theory]
        [InlineData(100.0, 15.0, 115.0)]
        [InlineData(680.0, 102.0, 782.0)]
        [InlineData(1695.0, 254.25, 1949.25)]
        public void CalcularISV15_BaseImponible_DebeCalcularImpuestoCorrecto(decimal baseGravada, decimal isvEsperado, decimal totalEsperado)
        {
            // Act
            decimal isv = Math.Round(baseGravada * 0.15m, 2);
            decimal total = baseGravada + isv;

            // Assert
            Assert.Equal(isvEsperado, isv);
            Assert.Equal(totalEsperado, total);
        }

        [Fact]
        public void FormatearNumeroFacturaSAR_DebeCompletar8Digitos()
        {
            // Arrange
            string prefijo = "000-001-01-";
            int correlativo = 41;

            // Act
            string numeroFactura = $"{prefijo}{correlativo:D8}";

            // Assert
            Assert.Equal("000-001-01-00000041", numeroFactura);
        }

        [Fact]
        public void ValidarRangoSAR_CorrelativoDentroDeRango_DebeSerValido()
        {
            // Arrange
            int rangoDesde = 1;
            int rangoHasta = 500000;
            int correlativoActual = 41;

            // Act
            bool esValido = correlativoActual >= rangoDesde && correlativoActual <= rangoHasta;

            // Assert
            Assert.True(esValido);
        }
    }
}
