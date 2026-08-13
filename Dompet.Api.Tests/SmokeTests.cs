using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dompet.Api.Tests;

public class SmokeTests
{
    [Fact]
    public async Task App_Boots_AndReturns404_ForUnknownRoute()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/register");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Factory_ResolvesSqliteDbContext()
    {
        using var factory = new TestWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dompet.Api.Data.AppDbContext>();

        Assert.True(db.Database.CanConnect());
        Assert.True(db.Database.IsSqlite());
    }
}
