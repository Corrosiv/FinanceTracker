using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceTracker.Tests;

public class ExceptionHandlingMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExceptionHandlingMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UniqueConstraint_ShouldReturn_409Conflict_WithExpectedJson()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.Configure(app =>
            {
                app.UseStatusCodePages();
                app.UseMiddleware<FinanceTracker.API.Middleware.ExceptionHandlingMiddleware>();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/test/throw-unique", context =>
                    {
                        throw new DbUpdateException("update failed", new Exception("UNIQUE constraint failed: Budget"));
                    });
                });
            });
        }).CreateClient();

        var res = await client.GetAsync("/test/throw-unique");
        var body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("A record with the same key already exists.", body);
    }

    [Fact]
    public async Task ForeignKeyViolation_ShouldReturn_400BadRequest_WithExpectedJson()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.Configure(app =>
            {
                app.UseStatusCodePages();
                app.UseMiddleware<FinanceTracker.API.Middleware.ExceptionHandlingMiddleware>();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/test/throw-fk", context =>
                    {
                        throw new DbUpdateException("update failed", new Exception("FOREIGN KEY constraint failed"));
                    });
                });
            });
        }).CreateClient();

        var res = await client.GetAsync("/test/throw-fk");
        var body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("A referenced record does not exist.", body);
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn_500InternalServerError_WithExpectedJson()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.Configure(app =>
            {
                app.UseStatusCodePages();
                app.UseMiddleware<FinanceTracker.API.Middleware.ExceptionHandlingMiddleware>();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/test/throw-unhandled", context =>
                    {
                        throw new Exception("boom");
                    });
                });
            });
        }).CreateClient();

        var res = await client.GetAsync("/test/throw-unhandled");
        var body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
        Assert.Contains("An unexpected error occurred.", body);
    }

    [Fact]
    public async Task NotFound_ShouldReturn_404WithJsonBody()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/v1/non-existent-route");
        var body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Contains("Resource not found.", body);
    }
}
