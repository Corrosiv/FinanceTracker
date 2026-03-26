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

public class ExpensesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExpensesControllerTests(WebApplicationFactory<Program> factory)
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

                var dbName = "ExpensesTestDb_" + Guid.NewGuid();
                services.AddDbContext<FinanceDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });

            builder.ConfigureServices(services =>
            {
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                db.Database.EnsureCreated();

                var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                db.Users.Add(new User { Id = userId, Name = "Test User", CreatedAt = DateTime.UtcNow });
                db.Imports.Add(new Import
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    UserId = userId,
                    FileName = "manual-entry",
                    UploadDate = DateTime.UtcNow,
                    Status = ImportStatus.Completed
                });
                db.SaveChanges();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/expenses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 25.50m,
            Description = "Lunch",
            Date = new DateTime(2026, 3, 20)
        };
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.NotNull(created);
        Assert.Equal(25.50m, created!.Amount); // DTO returns absolute value
        Assert.Equal("Lunch", created.Description);
    }

    [Fact]
    public async Task Create_ZeroAmount_ReturnsBadRequest()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 0,
            Description = "Invalid",
            Date = new DateTime(2026, 3, 20)
        };
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingDescription_ReturnsBadRequest()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "",
            Date = new DateTime(2026, 3, 20)
        };
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingExpense_ReturnsOk()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 15m,
            Description = "Coffee",
            Date = new DateTime(2026, 3, 21)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/expenses", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        var response = await _client.GetAsync($"/api/v1/expenses/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.Equal("Coffee", fetched!.Description);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/expenses/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingExpense_ReturnsOk()
    {
        var createDto = new CreateExpenseDto
        {
            Amount = 20m,
            Description = "Taxi",
            Date = new DateTime(2026, 3, 22)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/expenses", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        var updateDto = new UpdateExpenseDto { Description = "Uber ride" };
        var response = await _client.PutAsJsonAsync($"/api/v1/expenses/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.Equal("Uber ride", updated!.Description);
        Assert.Equal(20m, updated.Amount); // unchanged
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var updateDto = new UpdateExpenseDto { Amount = 5m };
        var response = await _client.PutAsJsonAsync($"/api/v1/expenses/{Guid.NewGuid()}", updateDto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingExpense_ReturnsNoContent()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "Delete me",
            Date = new DateTime(2026, 3, 23)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/expenses", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        var response = await _client.DeleteAsync($"/api/v1/expenses/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/expenses/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/expenses/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
