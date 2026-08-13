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
    public async Task<ActionResult<AnalyticsSummary>> Summary([FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _analytics.GetSummaryAsync(UserId, year, month));

    [HttpGet("by-category")]
    public async Task<ActionResult<List<CategoryBreakdown>>> ByCategory([FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await _analytics.GetByCategoryAsync(UserId, year, month));

    [HttpGet("monthly-trend")]
    public async Task<ActionResult<List<MonthlyTrend>>> MonthlyTrend([FromQuery] int months = 6) =>
        Ok(await _analytics.GetMonthlyTrendAsync(UserId, months));
}
