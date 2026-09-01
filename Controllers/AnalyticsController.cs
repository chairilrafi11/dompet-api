using System.Security.Claims;
using Dompet.Api.DTOs;
using Dompet.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dompet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummary>> Summary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _analytics.GetSummaryAsync(UserId, from, to, year, month));

    [HttpGet("by-category")]
    public async Task<ActionResult<List<CategoryBreakdown>>> ByCategory(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _analytics.GetByCategoryAsync(UserId, from, to, year, month));

    [HttpGet("trend")]
    public async Task<ActionResult<List<TrendPoint>>> Trend(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _analytics.GetTrendAsync(UserId, from, to, year, month));

    [HttpGet("wallet-recap")]
    public async Task<ActionResult<List<WalletRecap>>> WalletRecap(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _analytics.GetWalletRecapAsync(UserId, from, to, year, month));
}
