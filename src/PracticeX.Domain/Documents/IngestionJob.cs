using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class IngestionJob : Entity
{
    public Guid TenantId { get; set; }
    public Guid BatchId { get; set; }
    public Guid? SourceObjectId { get; set; }
    public Guid? DocumentAssetId { get; set; }
    public string Status { get; set; } = "queued";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
}

