using SistemaVisionTech.Common;
using Xunit;

namespace SistemaVisionTech.Tests
{
    public class EncryptionHelperTests
    {
        [Theory]
        [InlineData("Admin123!")]
        [InlineData("PasswordSuperSegura2026#")]
        [InlineData("CadenaConCaracteresEspeciales: áéíóú ñ $$ % & @")]
        public void Encrypt_And_Decrypt_ShouldReturnOriginalString(string plainText)
        {
            // Act
            string encrypted = EncryptionHelper.Encrypt(plainText);
            string decrypted = EncryptionHelper.Decrypt(encrypted);

            // Assert
            Assert.NotNull(encrypted);
            Assert.StartsWith("enc:v1:", encrypted);
            Assert.NotEqual(plainText, encrypted);
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Decrypt_LegacyPlainText_ShouldReturnOriginalString()
        {
            // Arrange
            string legacyText = "TextoPlanoLegadoSinCifrar";

            // Act
            string result = EncryptionHelper.Decrypt(legacyText);

            // Assert
            Assert.Equal(legacyText, result);
        }

        [Fact]
        public void Decrypt_CorruptedCiphertext_ShouldReturnMaskedFallback()
        {
            // Arrange: prefijo correcto pero bytes manipulados/corruptos
            string corrupted = "enc:v1:AAAAAAAABBBBBBBBCCCCCCCCDDDDDDDD";

            // Act
            string result = EncryptionHelper.Decrypt(corrupted);

            // Assert
            Assert.Equal("••••••••", result);
        }

        [Fact]
        public void Encrypt_EmptyOrNull_ShouldReturnSame()
        {
            Assert.Null(EncryptionHelper.Encrypt(null!));
            Assert.Equal(string.Empty, EncryptionHelper.Encrypt(string.Empty));
        }
    }
}
