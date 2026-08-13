using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Dompet.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenService _tokens;

    public AuthService(UserManager<ApplicationUser> users, JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<(bool, string?, AuthResponse?)> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)), null);

        return (true, null, ToResponse(user));
    }

    public async Task<(bool, string?, AuthResponse?)> LoginAsync(LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null)
            return (false, "Invalid credentials", null);

        if (!await _users.CheckPasswordAsync(user, request.Password))
            return (false, "Invalid credentials", null);

        return (true, null, ToResponse(user));
    }

    private AuthResponse ToResponse(ApplicationUser user) =>
        new(_tokens.CreateToken(user), user.Email!, user.DisplayName);
}
