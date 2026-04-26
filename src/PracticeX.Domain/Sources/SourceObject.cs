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

    public string ObjectKind { get; set; } = SourceObjectKinds.File;
    public string? RelativePath { get; set; }
    public string? ParentExternalId { get; set; }
    public long? SizeBytes { get; set; }
    public string? MetadataJson { get; set; }
    public string? ProposedStatus { get; set; }
    public string? QuickFingerprint { get; set; }
}

public static class SourceObjectProposedStatuses
{
    public const string Proposed = "proposed";
    public const string Selected = "selected";
    public const string Skipped = "skipped";
    public const string Uploaded = "uploaded";
}

public static class SourceObjectKinds
{
    public const string File = "file";
    public const string Folder = "folder";
    public const string MailMessage = "mail_message";
    public const string MailAttachment = "mail_attachment";
}
