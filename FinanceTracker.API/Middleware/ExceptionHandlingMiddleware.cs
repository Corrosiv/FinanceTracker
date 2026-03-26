using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                var body = JsonSerializer.Serialize(new { error = "A record with the same key already exists." });
                await context.Response.WriteAsync(body);
            }
            catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var body = JsonSerializer.Serialize(new { error = "A referenced record does not exist." });
                await context.Response.WriteAsync(body);
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                var body = JsonSerializer.Serialize(new { error = "An unexpected error occurred." });
                await context.Response.WriteAsync(body);
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? "";
            return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsForeignKeyViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? "";
            return message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
