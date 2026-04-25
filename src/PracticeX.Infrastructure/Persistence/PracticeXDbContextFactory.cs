using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PracticeX.Infrastructure.Persistence;

public sealed class PracticeXDbContextFactory : IDesignTimeDbContextFactory<PracticeXDbContext>
{
    public PracticeXDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PRACTICEX_DATABASE")
            ?? "Host=localhost;Port=5432;Database=practicex;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<PracticeXDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsHistoryTable("__ef_migrations_history", "audit"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PracticeXDbContext(options);
    }
}

