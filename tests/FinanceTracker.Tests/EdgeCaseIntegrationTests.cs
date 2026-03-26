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

public class EdgeCaseIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EdgeCaseIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<FinanceDbContext>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                var dbName = "EdgeCaseTestDb_" + Guid.NewGuid();
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

    // ── Categories Edge Cases ──────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_NullDescription_ReturnsCreated()
    {
        var dto = new CreateCategoryDto { Name = "NullDescCat" };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        Assert.Null(created!.Description);
    }

    [Fact]
    public async Task CreateCategory_WhitespaceOnlyName_ReturnsBadRequest()
    {
        var dto = new CreateCategoryDto { Name = "   \t  " };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_NameExactly255Chars_ReturnsCreated()
    {
        var dto = new CreateCategoryDto { Name = new string('X', 255) };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        Assert.Equal(255, created!.Name.Length);
    }

    [Fact]
    public async Task CreateCategory_NameExceeds255Chars_ReturnsBadRequest()
    {
        var dto = new CreateCategoryDto { Name = new string('X', 256) };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithLinkedExpenses_ExpensesSurvive()
    {
        // Create category
        var catDto = new CreateCategoryDto { Name = "LinkedCat" };
        var catResponse = await _client.PostAsJsonAsync("/api/v1/categories", catDto);
        var cat = await catResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Create expense linked to that category
        var expDto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "Linked expense",
            Date = new DateTime(2026, 6, 1),
            CategoryId = cat!.Id
        };
        var expResponse = await _client.PostAsJsonAsync("/api/v1/expenses", expDto);
        var expense = await expResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        // Delete the category
        var deleteResponse = await _client.DeleteAsync($"/api/v1/categories/{cat.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Expense should still exist
        var getExpense = await _client.GetAsync($"/api/v1/expenses/{expense!.Id}");
        Assert.Equal(HttpStatusCode.OK, getExpense.StatusCode);
    }

    // ── Expenses Edge Cases ────────────────────────────────────────────

    [Fact]
    public async Task CreateExpense_NegativeAmount_ReturnsAbsoluteValue()
    {
        var dto = new CreateExpenseDto
        {
            Amount = -30m,
            Description = "Negative input",
            Date = new DateTime(2026, 4, 1)
        };
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.Equal(30m, created!.Amount);
    }

    [Fact]
    public async Task CreateExpense_WithCategoryId_ReflectedInResponse()
    {
        var catDto = new CreateCategoryDto { Name = "WithCatEdge" };
        var catResponse = await _client.PostAsJsonAsync("/api/v1/categories", catDto);
        var cat = await catResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var dto = new CreateExpenseDto
        {
            Amount = 20m,
            Description = "Categorized expense",
            Date = new DateTime(2026, 4, 2),
            CategoryId = cat!.Id
        };
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.Equal(cat.Id, created!.CategoryId);
    }

    [Fact]
    public async Task CreateExpense_NullCategoryId_ReturnsCreatedUncategorized()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 5m,
            Description = "No category",
            Date = new DateTime(2026, 4, 3),
            CategoryId = null
        };
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.Null(created!.CategoryId);
    }

    [Fact]
    public async Task UpdateExpense_ZeroAmount_IsAccepted()
    {
        var createDto = new CreateExpenseDto
        {
            Amount = 15m,
            Description = "UpdateZeroTest",
            Date = new DateTime(2026, 4, 4)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/expenses", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        // No validator on the update path — zero amount goes through
        var updateDto = new UpdateExpenseDto { Amount = 0m };
        var response = await _client.PutAsJsonAsync($"/api/v1/expenses/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExpense_EmptyDescription_IsAccepted()
    {
        var createDto = new CreateExpenseDto
        {
            Amount = 12m,
            Description = "UpdateEmptyDescTest",
            Date = new DateTime(2026, 4, 5)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/expenses", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        // No validator on the update path — empty description goes through
        var updateDto = new UpdateExpenseDto { Description = "" };
        var response = await _client.PutAsJsonAsync($"/api/v1/expenses/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExpense_NegativeAmount_ReturnsAbsoluteValue()
    {
        var createDto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "NegateUpdateTest",
            Date = new DateTime(2026, 4, 6)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/expenses", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponseDto>();

        var updateDto = new UpdateExpenseDto { Amount = -50m };
        var response = await _client.PutAsJsonAsync($"/api/v1/expenses/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponseDto>();
        Assert.Equal(50m, updated!.Amount);
    }
}
