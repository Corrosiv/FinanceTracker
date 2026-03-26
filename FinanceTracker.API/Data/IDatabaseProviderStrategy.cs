using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Data;

public interface IDatabaseProviderStrategy
{
    void Configure(DbContextOptionsBuilder options, string connectionString);
}
