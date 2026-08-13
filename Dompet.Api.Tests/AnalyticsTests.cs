using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Xunit;

namespace Dompet.Api.Tests;

public class AnalyticsTests
{
    private static async Task<HttpClient> ClientAsync()
    {
        var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("an@b.com", "Password123!", "AN"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var wallet = await client.PostAsJsonAsync("/api/wallets", new WalletRequest("Cash", 0));
        var walletId = (await wallet.Content.ReadFromJsonAsync<WalletDto>())!.Id;
        var inc = await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Gaji", CategoryType.Income));
        var incId = (await inc.Content.ReadFromJsonAsync<CategoryDto>())!.Id;
        var exp = await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Makan", CategoryType.Expense));
        var expId = (await exp.Content.ReadFromJsonAsync<CategoryDto>())!.Id;

        await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, incId, 100000, TransactionType.Income, null, DateTimeOffset.UtcNow));
        await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, expId, 40000, TransactionType.Expense, null, DateTimeOffset.UtcNow));

        return client;
    }

    [Fact]
    public async Task Summary_ComputesIncomeExpenseNet()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/analytics/summary");
        var summary = await response.Content.ReadFromJsonAsync<AnalyticsSummary>();

        Assert.Equal(100000m, summary!.Income);
        Assert.Equal(40000m, summary.Expense);
        Assert.Equal(60000m, summary.Net);
    }

    [Fact]
    public async Task ByCategory_ReturnsBreakdown()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/analytics/by-category");
        var breakdown = await response.Content.ReadFromJsonAsync<List<CategoryBreakdown>>();

        var item = Assert.Single(breakdown!);
        Assert.Equal("Makan", item.Category);
        Assert.Equal(40000m, item.Amount);
    }

    [Fact]
    public async Task MonthlyTrend_ReturnsCurrentMonth()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/analytics/monthly-trend?months=3");
        var trend = await response.Content.ReadFromJsonAsync<List<MonthlyTrend>>();

        Assert.Equal(3, trend!.Count);
        var current = trend[^1];
        Assert.Equal(100000m, current.Income);
        Assert.Equal(40000m, current.Expense);
    }
}
