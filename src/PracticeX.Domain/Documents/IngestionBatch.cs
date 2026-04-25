using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class IngestionBatch : Entity
{
    public Guid TenantId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = "pending";
    public int FileCount { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

