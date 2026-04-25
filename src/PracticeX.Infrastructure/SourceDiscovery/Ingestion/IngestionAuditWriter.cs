using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Ingestion;
using PracticeX.Domain.Audit;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure.SourceDiscovery.Ingestion;

public sealed class IngestionAuditWriter(PracticeXDbContext dbContext, IClock clock) : IIngestionAuditWriter
{
    public async Task WriteAsync(IngestionAuditEntry entry, CancellationToken cancellationToken)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            TenantId = entry.TenantId,
            ActorType = entry.ActorType,
            ActorId = entry.ActorId,
            EventType = entry.EventType,
            ResourceType = entry.ResourceType,
            ResourceId = entry.ResourceId,
            MetadataJson = entry.MetadataJson,
            CreatedAt = clock.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
