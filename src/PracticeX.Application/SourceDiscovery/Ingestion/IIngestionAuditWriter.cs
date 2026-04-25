namespace PracticeX.Application.SourceDiscovery.Ingestion;

public interface IIngestionAuditWriter
{
    Task WriteAsync(IngestionAuditEntry entry, CancellationToken cancellationToken);
}

public sealed class IngestionAuditEntry
{
    public required Guid TenantId { get; init; }
    public required Guid? ActorId { get; init; }
    public required string ActorType { get; init; }
    public required string EventType { get; init; }
    public required string ResourceType { get; init; }
    public required Guid ResourceId { get; init; }
    public string? MetadataJson { get; init; }
}
