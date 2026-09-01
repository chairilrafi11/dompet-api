using Dompet.Api.Data;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    private static (DateTime Start, DateTime End) MonthRange(int? year, int? month)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        var start = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return (start, end);
    }

    public async Task<AnalyticsSummary> GetSummaryAsync(
        string userId,
        DateTime? from,
        DateTime? to,
        int? year,
        int? month)
    {
        var (start, end) = ResolveRange(from, to, year, month);
        var (prevStart, prevEnd) = PreviousRange(start, end);

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(r => (r.Type, r.Amount)).ToList());

        var prevRows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= prevStart && t.Date < prevEnd)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(r => (r.Type, r.Amount)).ToList());

        var income = SumByType(rows, TransactionType.Income);
        var expense = SumByType(rows, TransactionType.Expense);
        var prevIncome = SumByType(prevRows, TransactionType.Income);
        var prevExpense = SumByType(prevRows, TransactionType.Expense);

        return new AnalyticsSummary(income, expense, income - expense, prevIncome, prevExpense, prevIncome - prevExpense);
    }

    public async Task<List<CategoryBreakdown>> GetByCategoryAsync(
        string userId,
        DateTime? from,
        DateTime? to,
        int? year,
        int? month)
    {
        var (start, end) = ResolveRange(from, to, year, month);
        var (prevStart, prevEnd) = PreviousRange(start, end);

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end && t.Type == TransactionType.Expense)
            .Select(t => new { t.Category.Id, t.Category.Name, t.Amount })
            .ToListAsync();

        var prevRows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= prevStart && t.Date < prevEnd && t.Type == TransactionType.Expense)
            .Select(t => new { t.Category.Name, t.Amount })
            .ToListAsync();

        var prevByName = prevRows
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return rows
            .GroupBy(t => new { t.Id, t.Name })
            .Select(g => new CategoryBreakdown(g.Key.Id, g.Key.Name, g.Sum(t => t.Amount), prevByName.GetValueOrDefault(g.Key.Name)))
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    public async Task<List<TrendPoint>> GetTrendAsync(
        string userId,
        DateTime? from,
        DateTime? to,
        int? year,
        int? month)
    {
        var (start, end) = ResolveRange(from, to, year, month);
        if (start >= end)
            return new List<TrendPoint>();

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end)
            .Select(t => new { t.Type, t.Amount, t.Date })
            .ToListAsync();

        var isDaily = (end - start).TotalDays <= 31;
        var result = new List<TrendPoint>();

        if (isDaily)
        {
            var map = rows
                .GroupBy(t => t.Date.Date)
                .ToDictionary(
                    g => $"{g.Key.Year:D4}-{g.Key.Month:D2}-{g.Key.Day:D2}",
                    g => (Income: g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                          Expense: g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)));

            for (var d = start.Date; d < end.Date; d = d.AddDays(1))
            {
                var key = $"{d.Year:D4}-{d.Month:D2}-{d.Day:D2}";
                var (income, expense) = map.TryGetValue(key, out var v) ? v : (0m, 0m);
                result.Add(new TrendPoint(key, income, expense));
            }
        }
        else
        {
            var map = rows
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .ToDictionary(
                    g => $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    g => (Income: g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                          Expense: g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)));

            var monthCursor = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            while (monthCursor < end)
            {
                var key = $"{monthCursor.Year:D4}-{monthCursor.Month:D2}";
                var (income, expense) = map.TryGetValue(key, out var v) ? v : (0m, 0m);
                result.Add(new TrendPoint(key, income, expense));
                monthCursor = monthCursor.AddMonths(1);
            }
        }

        return result;
    }

    public async Task<List<WalletRecap>> GetWalletRecapAsync(
        string userId,
        DateTime? from,
        DateTime? to,
        int? year,
        int? month)
    {
        var (start, end) = ResolveRange(from, to, year, month);

        var rows = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end)
            .Select(t => new { t.Wallet.Id, t.Wallet.Name, t.Type, t.Amount })
            .ToListAsync();

        return rows
            .GroupBy(t => new { t.Id, t.Name })
            .Select(g => new WalletRecap(
                g.Key.Id,
                g.Key.Name,
                g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) -
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)))
            .OrderByDescending(x => x.Net)
            .ToList();
    }

    private static decimal SumByType(IEnumerable<(TransactionType Type, decimal Amount)> rows, TransactionType type)
        => rows.Where(r => r.Type == type).Sum(r => r.Amount);

    private static (DateTime Start, DateTime End) ResolveRange(
        DateTime? from, DateTime? to, int? year, int? month)
    {
        if (from.HasValue && to.HasValue)
            return (from.Value, to.Value);

        var (start, end) = MonthRange(year, month);
        if (from.HasValue)
            return (from.Value, end);
        if (to.HasValue)
            return (start, to.Value);

        return (start, end);
    }

    private static (DateTime Start, DateTime End) PreviousRange(DateTime start, DateTime end)
    {
        var span = end - start;
        return (start.AddTicks(-span.Ticks), start);
    }
}
