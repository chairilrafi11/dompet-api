using Dompet.Api.DTOs;

namespace Dompet.Api.Services;

public interface IAnalyticsService
{
    Task<AnalyticsSummary> GetSummaryAsync(string userId, DateTime? from, DateTime? to, int? year, int? month);
    Task<List<CategoryBreakdown>> GetByCategoryAsync(string userId, DateTime? from, DateTime? to, int? year, int? month);
    Task<List<TrendPoint>> GetTrendAsync(string userId, DateTime? from, DateTime? to, int? year, int? month);
    Task<List<WalletRecap>> GetWalletRecapAsync(string userId, DateTime? from, DateTime? to, int? year, int? month);
}
