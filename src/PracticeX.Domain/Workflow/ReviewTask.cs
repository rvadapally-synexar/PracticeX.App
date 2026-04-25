using PracticeX.Domain.Common;

namespace PracticeX.Domain.Workflow;

public sealed class ReviewTask : Entity
{
    public Guid TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string Decision { get; set; } = "pending";
    public DateTimeOffset? ResolvedAt { get; set; }
    public int? EffortSeconds { get; set; }
}

