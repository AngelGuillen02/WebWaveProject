using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Auth;

namespace SistemaVisionTech.Features.Acceso.Interfaces
{
    public interface IAuthService
    {
        Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
    }
}
