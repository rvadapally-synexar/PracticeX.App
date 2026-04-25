using PracticeX.Domain.Common;

namespace PracticeX.Domain.Audit;

public sealed class AuditEvent : Entity
{
    public Guid TenantId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string? PriorValueHash { get; set; }
    public string? NewValueHash { get; set; }
    public string? MetadataJson { get; set; }
}

