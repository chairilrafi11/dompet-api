using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Xunit;

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
            new TransactionRequest(walletId, catId, 50000, TransactionType.Expense, "Nasi", DateTimeOffset.UtcNow));

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
            new TransactionRequest(walletId, catId, 10000, TransactionType.Income, null, DateTimeOffset.UtcNow));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_FilterByCategory()
    {
        var (client, walletId, catId) = await SetupAsync();
        await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, catId, 50000, TransactionType.Expense, null, DateTimeOffset.UtcNow));

        var response = await client.GetAsync($"/api/transactions?categoryId={catId}");
        var list = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();

        Assert.Single(list!);
        Assert.Equal("Makan", list![0].CategoryName);
    }
}
