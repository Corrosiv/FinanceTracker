using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Data;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var strategy = ResolveProvider(provider);

        services.AddDbContext<FinanceDbContext>(options => strategy.Configure(options, connectionString));

        return services;
    }

    private static IDatabaseProviderStrategy ResolveProvider(string providerName) => providerName switch
    {
        "Sqlite" => new SqliteDatabaseProvider(),
        // To add a new provider:
        // 1. Install the NuGet package (e.g. Npgsql.EntityFrameworkCore.PostgreSQL)
        // 2. Create a class implementing IDatabaseProviderStrategy
        // 3. Add a case here (e.g. "PostgreSql" => new PostgreSqlDatabaseProvider())
        // 4. Update appsettings.json with the provider name and connection string
        // 5. Re-generate migrations: dotnet ef migrations add InitialCreate
        _ => throw new InvalidOperationException(
            $"Unsupported database provider: '{providerName}'. Supported: Sqlite.")
    };
}
