namespace PracticeX.Application.SourceDiscovery.Connectors;

public sealed class DiscoveryRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ConnectionId { get; init; }
    public required Guid InitiatedByUserId { get; init; }
    public DateTimeOffset? Since { get; init; }
    public int? MaxItems { get; init; }
    public IReadOnlyCollection<DiscoveryInput>? Inputs { get; init; }
    public string? FolderHint { get; init; }
}

/// <summary>
/// Generic input shape used when the connector takes externally supplied items
/// (e.g. uploaded files for the local folder connector). Streams are owned by
/// the caller and disposed after the request completes.
/// </summary>
public sealed class DiscoveryInput
{
    public required string Name { get; init; }
    public string? RelativePath { get; init; }
    public required string MimeType { get; init; }
    public required Stream Content { get; init; }
    public long? SizeBytes { get; init; }
}
