using System.Text.Json.Serialization;

namespace SistemaVisionTech.Features.Acceso.Dtos.Auth
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;

        public string Contraseña { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string? Password
        {
            get => Contraseña;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Contraseña = value;
            }
        }
    }
}
