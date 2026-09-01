using Dompet.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var dbOk = await _db.Database.CanConnectAsync();
            return dbOk
                ? Ok(new { status = "ok" })
                : StatusCode(503, new { status = "db_unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "db_unavailable", error = ex.GetType().Name + ": " + ex.Message });
        }
    }
}
