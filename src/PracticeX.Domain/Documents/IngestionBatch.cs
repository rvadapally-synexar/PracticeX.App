using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class IngestionBatch : Entity
{
    public Guid TenantId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceConnectionId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = IngestionBatchStatus.Pending;
    public int FileCount { get; set; }
    public int CandidateCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public static class IngestionBatchStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string PartialSuccess = "partial_success";
}
