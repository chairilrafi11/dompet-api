using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dompet.Api.DTOs;
using Xunit;

namespace Dompet.Api.Tests;

public class WalletTests
{
    private static async Task<HttpClient> AuthenticatedClientAsync()
    {
        var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("w@b.com", "Password123!", "W"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task CreateWallet_ReturnsCreated()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/wallets",
            new WalletRequest("Cash", 100000));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WalletDto>();
        Assert.Equal("Cash", body!.Name);
        Assert.Equal(100000m, body.Balance);
    }

    [Fact]
    public async Task GetWallets_ReturnsOnlyOwnWallets()
    {
        var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var reg1 = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("u1@b.com", "Password123!", "U1"));
        var t1 = (await reg1.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t1);
        await client.PostAsJsonAsync("/api/wallets", new WalletRequest("Mine", 0));

        var reg2 = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("u2@b.com", "Password123!", "U2"));
        var t2 = (await reg2.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t2);

        var response = await client.GetAsync("/api/wallets");
        var wallets = await response.Content.ReadFromJsonAsync<List<WalletDto>>();

        Assert.Empty(wallets!);
    }

    [Fact]
    public async Task DeleteWallet_NotOwned_ReturnsNotFound()
    {
        var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var reg1 = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("o1@b.com", "Password123!", "O1"));
        var t1 = (await reg1.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t1);
        var created = await client.PostAsJsonAsync("/api/wallets", new WalletRequest("Mine", 0));
        var wallet = await created.Content.ReadFromJsonAsync<WalletDto>();

        var reg2 = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("o2@b.com", "Password123!", "O2"));
        var t2 = (await reg2.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t2);

        var response = await client.DeleteAsync($"/api/wallets/{wallet!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
