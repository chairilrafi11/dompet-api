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
            new TransactionRequest(walletId, incId, 100000, TransactionType.Income, null, DateTime.UtcNow));
        await client.PostAsJsonAsync("/api/transactions",
            new TransactionRequest(walletId, expId, 40000, TransactionType.Expense, null, DateTime.UtcNow));

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
    public async Task Summary_IncludesPreviousPeriod()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/analytics/summary");
        var summary = await response.Content.ReadFromJsonAsync<AnalyticsSummary>();

        Assert.Equal(0m, summary!.PrevIncome);
        Assert.Equal(0m, summary.PrevExpense);
        Assert.Equal(0m, summary.PrevNet);
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
        Assert.True(item.CategoryId > 0);
    }

    [Fact]
    public async Task Trend_ReturnsCurrentMonth()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/analytics/trend");
        var trend = await response.Content.ReadFromJsonAsync<List<TrendPoint>>();

        Assert.NotNull(trend);
        Assert.NotEmpty(trend);
        Assert.Equal(100000m, trend.Sum(t => t.Income));
        Assert.Equal(40000m, trend.Sum(t => t.Expense));    }

    [Fact]
    public async Task WalletRecap_ReturnsWalletSummary()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/analytics/wallet-recap");
        var recap = await response.Content.ReadFromJsonAsync<List<WalletRecap>>();

        var item = Assert.Single(recap!);
        Assert.Equal("Cash", item.WalletName);
        Assert.Equal(100000m, item.Income);
        Assert.Equal(40000m, item.Expense);
        Assert.Equal(60000m, item.Net);
    }
}
