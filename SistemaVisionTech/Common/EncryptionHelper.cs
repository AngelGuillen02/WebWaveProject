using System.Security.Cryptography;
using System.Text;

namespace SistemaVisionTech.Common
{
    public static class EncryptionHelper
    {
        private const string Prefix = "enc:v1:";
        private static readonly byte[] DefaultKey = SHA256.HashData(Encoding.UTF8.GetBytes(
            Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
            ?? Environment.GetEnvironmentVariable("JWT_KEY")
            ?? "VisionTech_Aes256_Gcm_StaticMasterKey_2026_Secure!"
        ));

        /// <summary>
        /// Cifra una cadena usando AES-256-GCM con un nonce aleatorio de 12 bytes y un tag de autenticación de 16 bytes.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
            RandomNumberGenerator.Fill(nonce);

            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

            using (var aesGcm = new AesGcm(DefaultKey, AesGcm.TagByteSizes.MaxSize))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // Formato paquete: [nonce (12)] + [tag (16)] + [ciphertext (N)]
            byte[] payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);

            return Prefix + Convert.ToBase64String(payload);
        }

        /// <summary>
        /// Descifra un paquete AES-256-GCM. Si no está cifrado con el prefijo, devuelve el texto tal cual.
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
                return cipherText; // Texto plano legado

            try
            {
                string base64Payload = cipherText[Prefix.Length..];
                byte[] payload = Convert.FromBase64String(base64Payload);

                int nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
                int tagSize = AesGcm.TagByteSizes.MaxSize; // 16
                int cipherSize = payload.Length - nonceSize - tagSize;

                if (cipherSize < 0)
                    return "••••••••";

                byte[] nonce = new byte[nonceSize];
                byte[] tag = new byte[tagSize];
                byte[] cipherBytes = new byte[cipherSize];

                Buffer.BlockCopy(payload, 0, nonce, 0, nonceSize);
                Buffer.BlockCopy(payload, nonceSize, tag, 0, tagSize);
                Buffer.BlockCopy(payload, nonceSize + tagSize, cipherBytes, 0, cipherSize);

                byte[] plainBytes = new byte[cipherSize];
                using (var aesGcm = new AesGcm(DefaultKey, tagSize))
                {
                    aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
                }

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return "••••••••";
            }
        }
    }
}
