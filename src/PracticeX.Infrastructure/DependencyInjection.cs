using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PracticeX")
            ?? "Host=localhost;Port=5432;Database=practicex;Username=postgres;Password=postgres";

        services.AddDbContext<PracticeXDbContext>(options =>
            options
                .UseNpgsql(connectionString, postgres =>
                    postgres.MigrationsHistoryTable("__ef_migrations_history", "audit"))
                .UseSnakeCaseNamingConvention());

        return services;
    }
}

