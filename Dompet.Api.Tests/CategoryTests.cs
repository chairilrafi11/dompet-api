using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Xunit;

namespace Dompet.Api.Tests;

public class CategoryTests
{
    private static async Task<HttpClient> ClientAsync()
    {
        var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("c@b.com", "Password123!", "C"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task CreateCategory_ReturnsCreated()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/categories",
            new CategoryRequest("Makan", CategoryType.Expense));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Makan", body!.Name);
        Assert.Equal(CategoryType.Expense, body.Type);
    }

    [Fact]
    public async Task GetCategories_FilterByType()
    {
        var client = await ClientAsync();
        await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Gaji", CategoryType.Income));
        await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Makan", CategoryType.Expense));

        var response = await client.GetAsync("/api/categories?type=Income");
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        Assert.Single(categories!);
        Assert.Equal("Gaji", categories![0].Name);
    }
}
