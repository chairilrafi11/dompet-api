using Dompet.Api.Data;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    private static (DateTimeOffset Start, DateTimeOffset End) MonthRange(int? year, int? month)
    {
        var now = DateTimeOffset.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        var start = new DateTimeOffset(y, m, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        return (start, end);
    }

    public async Task<AnalyticsSummary> GetSummaryAsync(string userId, int? year, int? month)
    {
        var (start, end) = MonthRange(year, month);

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Type, t.Amount, t.Date })
            .ToListAsync();

        var inMonth = rows.Where(t => t.Date >= start && t.Date < end).ToList();
        var income = inMonth.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = inMonth.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        return new AnalyticsSummary(income, expense, income - expense);
    }

    public async Task<List<CategoryBreakdown>> GetByCategoryAsync(string userId, int? year, int? month)
    {
        var (start, end) = MonthRange(year, month);

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Category.Name, t.Type, t.Amount, t.Date })
            .ToListAsync();

        return rows
            .Where(t => t.Date >= start && t.Date < end && t.Type == TransactionType.Expense)
            .GroupBy(t => t.Name)
            .Select(g => new CategoryBreakdown(g.Key, g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    public async Task<List<MonthlyTrend>> GetMonthlyTrendAsync(string userId, int months)
    {
        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-(months - 1));

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Type, t.Amount, t.Date })
            .ToListAsync();

        var groups = rows
            .Where(t => t.Date >= start)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .ToDictionary(
                g => g.Key,
                g => (
                    Income: g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expense: g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)));

        var result = new List<MonthlyTrend>();
        for (var i = 0; i < months; i++)
        {
            var d = start.AddMonths(i);
            var key = new { d.Year, d.Month };
            var (income, expense) = groups.TryGetValue(key, out var v) ? v : (0m, 0m);
            result.Add(new MonthlyTrend($"{d.Year:D4}-{d.Month:D2}", income, expense));
        }
        return result;
    }
}
