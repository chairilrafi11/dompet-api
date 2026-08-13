using Dompet.Api.DTOs;

namespace Dompet.Api.Services;

public interface IAnalyticsService
{
    Task<AnalyticsSummary> GetSummaryAsync(string userId, int? year, int? month);
    Task<List<CategoryBreakdown>> GetByCategoryAsync(string userId, int? year, int? month);
    Task<List<MonthlyTrend>> GetMonthlyTrendAsync(string userId, int months);
}
