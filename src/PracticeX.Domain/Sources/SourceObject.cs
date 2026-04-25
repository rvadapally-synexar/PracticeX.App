using PracticeX.Domain.Common;

namespace PracticeX.Domain.Sources;

public sealed class SourceObject : Entity
{
    public Guid TenantId { get; set; }
    public Guid ConnectionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public DateTimeOffset? SourceCreatedAt { get; set; }
    public DateTimeOffset? SourceModifiedAt { get; set; }
}

