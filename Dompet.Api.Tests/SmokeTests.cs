using System.Net;
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
}
