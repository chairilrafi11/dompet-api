using Dompet.Api.DTOs;

namespace Dompet.Api.Services;

public interface IAuthService
{
    Task<(bool Succeeded, string? Error, AuthResponse? Data)> RegisterAsync(RegisterRequest request);
    Task<(bool Succeeded, string? Error, AuthResponse? Data)> LoginAsync(LoginRequest request);
}
