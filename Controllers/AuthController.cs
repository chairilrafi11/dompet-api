using Dompet.Api.DTOs;
using Dompet.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dompet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var (succeeded, error, data) = await _auth.RegisterAsync(request);
        return succeeded ? Ok(data) : Conflict(new { error });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var (succeeded, error, data) = await _auth.LoginAsync(request);
        return succeeded ? Ok(data) : Unauthorized(new { error });
    }
}
