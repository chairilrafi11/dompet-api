using System.Net;
using System.Net.Http.Json;
using Dompet.Api.DTOs;
using Xunit;

namespace Dompet.Api.Tests;

public class AuthTests
{
    [Fact]
    public async Task Register_ReturnsToken()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("a@b.com", "Password123!", "A"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var request = new RegisterRequest("dup@b.com", "Password123!", "Dup");

        await client.PostAsJsonAsync("/api/auth/register", request);
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("x@b.com", "Password123!", "X"));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("x@b.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("y@b.com", "Password123!", "Y"));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("y@b.com", "WrongPass123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
