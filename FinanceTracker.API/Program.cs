using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

var app = builder.Build();

// Ensure DB is created and seed default user + manual import for V1
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();

    var defaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    if (!db.Users.Any(u => u.Id == defaultUserId))
    {
        db.Users.Add(new FinanceTracker.API.Models.User
        {
            Id = defaultUserId,
            Name = "Default User",
            CreatedAt = DateTime.UtcNow
        });

        db.Imports.Add(new FinanceTracker.API.Models.Import
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            UserId = defaultUserId,
            FileName = "manual-entry",
            UploadDate = DateTime.UtcNow,
            Status = FinanceTracker.API.Models.ImportStatus.Completed
        });

        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class accessible to integration tests
public partial class Program { }