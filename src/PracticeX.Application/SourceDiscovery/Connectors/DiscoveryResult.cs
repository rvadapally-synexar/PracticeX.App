namespace PracticeX.Application.SourceDiscovery.Connectors;

public sealed class DiscoveryResult
{
    public required IReadOnlyList<DiscoveredItem> Items { get; init; }
    public required IReadOnlyList<DiscoveryNote> Notes { get; init; }
}

public sealed class DiscoveredItem
{
    public required string ExternalId { get; init; }
    public required string Name { get; init; }
    public required string MimeType { get; init; }
    public string? RelativePath { get; init; }
    public string? ParentExternalId { get; init; }
    public string ObjectKind { get; init; } = "file";
    public long? SizeBytes { get; init; }
    public string? Sha256 { get; init; }
    public string? Sha256OfContent { get; init; }
    public DateTimeOffset? SourceCreatedAt { get; init; }
    public DateTimeOffset? SourceModifiedAt { get; init; }
    public string? Uri { get; init; }
    public byte[]? InlineContent { get; init; }
    public string? MetadataJson { get; init; }

    public IReadOnlyList<string> Hints { get; init; } = [];
}

public sealed class DiscoveryNote
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string Severity { get; init; } = "info";
    public string? Subject { get; init; }
}
