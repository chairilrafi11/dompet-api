using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dompet.Api.DTOs;
using Dompet.Api.Models;

namespace Dompet.Api.Tests;

public class TransactionTests
{
    private static async Task<(HttpClient Client, int WalletId, int CatId)> SetupAsync()
    {
        var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("t@b.com", "Password123!", "T"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var wallet = await client.PostAsJsonAsync("/api/wallets", new WalletRequest("Cash", 0));
        var walletId = (await wallet.Content.ReadFromJsonAsync<WalletDto>())!.Id;
        var cat = await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Makan", CategoryType.Expense));
        var catId = (await cat.Content.ReadFromJsonAsync<CategoryDto>())!.Id;
        return (client, walletId, catId);
    }

    [Fact]
    public async Task CreateTransaction_UpdatesWalletBalance()
    {
        var (client, walletId, catId) = await SetupAsync();

        var create = await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, catId, 50000, TransactionType.Expense, "Nasi", DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var wallets = await client.GetAsync("/api/wallets");
        var list = await wallets.Content.ReadFromJsonAsync<List<WalletDto>>();
        Assert.Equal(-50000m, list![0].Balance);
    }

    [Fact]
    public async Task CreateTransaction_TypeMismatch_ReturnsBadRequest()
    {
        var (client, walletId, catId) = await SetupAsync();

        var response = await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, catId, 10000, TransactionType.Income, null, DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_FilterByCategory()
    {
        var (client, walletId, catId) = await SetupAsync();
        await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, catId, 50000, TransactionType.Expense, null, DateTime.UtcNow));

        var response = await client.GetAsync($"/api/transactions?categoryId={catId}");
        var list = (await response.Content.ReadFromJsonAsync<PageResult<TransactionDto>>())!.Items;

        Assert.Single(list!);
        Assert.Equal("Makan", list![0].CategoryName);
    }

    [Fact]
    public async Task GetTransactions_FilterByDateRange()
    {
        var (client, walletId, catId) = await SetupAsync();

        var recent = DateTime.UtcNow.AddDays(-1);
        var old = DateTime.UtcNow.AddDays(-10);

        await client.PostAsJsonAsync("/api/transactions", new TransactionRequest(walletId, catId, 50000, TransactionType.Expense, null, recent));
        await client.PostAsJsonAsync("/api/transactions", new TransactionRequest(walletId, catId, 20000, TransactionType.Expense, null, old));

        var respose = await client.GetAsync($"/api/transactions?dateFrom={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-5).ToString("O"))}");
        var list = (await respose.Content.ReadFromJsonAsync<PageResult<TransactionDto>>())!.Items;

        Assert.Single(list!);
        Assert.Equal(50000m, list![0].Amount);
    }

    [Fact]
    public async Task CreateTransaction_WithoutDate_UseNow()
    {
        var (client, walletId, catId) = await SetupAsync();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionRequest(walletId, catId, 50000, TransactionType.Expense, null, null)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.True((DateTime.UtcNow - body!.Date).Duration() < TimeSpan.FromMinutes(5));
    }

}
