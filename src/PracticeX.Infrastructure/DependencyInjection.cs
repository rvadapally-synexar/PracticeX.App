using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Classification;
using PracticeX.Application.SourceDiscovery.Connectors;
using PracticeX.Application.SourceDiscovery.Ingestion;
using PracticeX.Application.SourceDiscovery.Outlook;
using PracticeX.Application.SourceDiscovery.Storage;
using PracticeX.Infrastructure.Persistence;
using PracticeX.Infrastructure.SourceDiscovery.Connectors;
using PracticeX.Infrastructure.SourceDiscovery.Ingestion;
using PracticeX.Infrastructure.SourceDiscovery.Outlook;
using PracticeX.Infrastructure.SourceDiscovery.Storage;
using PracticeX.Infrastructure.Tenancy;

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

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUserContext, DemoCurrentUserContext>();

        services.Configure<DocumentStorageOptions>(configuration.GetSection(DocumentStorageOptions.SectionName));
        services.Configure<MicrosoftGraphOptions>(configuration.GetSection(MicrosoftGraphOptions.SectionName));

        services.AddSingleton<IDocumentClassifier, RuleBasedContractClassifier>();
        services.AddSingleton<IDocumentStorage, LocalFileSystemDocumentStorage>();
        services.AddSingleton<IMicrosoftGraphTokenStore, InMemoryMicrosoftGraphTokenStore>();

        services.AddHttpClient("microsoft-graph");
        services.AddHttpClient("microsoft-graph-token");

        services.AddScoped<IMicrosoftGraphOAuthService, MicrosoftGraphOAuthService>();
        services.AddScoped<IMicrosoftGraphClient, MicrosoftGraphClient>();

        services.AddScoped<ISourceConnector, LocalFolderConnector>();
        services.AddScoped<ISourceConnector, OutlookGraphConnector>();
        services.AddScoped<IConnectorRegistry, ConnectorRegistry>();
        services.AddScoped<IIngestionOrchestrator, IngestionOrchestrator>();
        services.AddScoped<IIngestionAuditWriter, IngestionAuditWriter>();

        return services;
    }
}
