using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;

namespace FinanceTracker.Tests;

public class CategoriesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CategoriesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext-related registrations to avoid dual-provider conflict
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<FinanceDbContext>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                // Add in-memory database
                var dbName = "CategoriesTestDb_" + Guid.NewGuid();
                services.AddDbContext<FinanceDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });

            builder.ConfigureServices(services =>
            {
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                db.Database.EnsureCreated();
                db.Users.Add(new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = "Test User",
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task WhenGettingAllCategories_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content.ReadFromJsonAsync<CategoryResponseDto[]>();
        Assert.NotNull(categories);
    }

    [Fact]
    public async Task WhenCreatingValidCategory_ShouldReturnCreated()
    {
        var dto = new CreateCategoryDto { Name = "Food", Description = "Groceries" };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        Assert.NotNull(created);
        Assert.Equal("Food", created!.Name);
        Assert.Equal("Groceries", created.Description);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task WhenCreatingCategoryWithEmptyName_ShouldReturnBadRequest()
    {
        var dto = new CreateCategoryDto { Name = "" };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenGettingExistingCategoryById_ShouldReturnOk()
    {
        var dto = new CreateCategoryDto { Name = "Transport" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var response = await _client.GetAsync($"/api/v1/categories/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        Assert.Equal("Transport", fetched!.Name);
    }

    [Fact]
    public async Task WhenGettingNonExistentCategoryById_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WhenDeletingExistingCategory_ShouldReturnNoContent()
    {
        var dto = new CreateCategoryDto { Name = "ToDelete" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var response = await _client.DeleteAsync($"/api/v1/categories/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/v1/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task WhenDeletingNonExistentCategory_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
