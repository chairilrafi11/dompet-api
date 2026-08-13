namespace Dompet.Api.DTOs;

public record AnalyticsSummary(decimal Income, decimal Expense, decimal Net);

public record CategoryBreakdown(string Category, decimal Amount);

public record MonthlyTrend(string Month, decimal Income, decimal Expense);
