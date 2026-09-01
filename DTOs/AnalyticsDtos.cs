namespace Dompet.Api.DTOs;

public record AnalyticsSummary(
    decimal Income,
    decimal Expense,
    decimal Net,
    decimal PrevIncome,
    decimal PrevExpense,
    decimal PrevNet);

public record CategoryBreakdown(int CategoryId, string Category, decimal Amount, decimal PrevAmount);

public record TrendPoint(string Date, decimal Income, decimal Expense);

public record WalletRecap(int WalletId, string WalletName, decimal Income, decimal Expense, decimal Net);
