using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Data;

public class SqliteDatabaseProvider : IDatabaseProviderStrategy
{
    public void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(connectionString);
    }
}
