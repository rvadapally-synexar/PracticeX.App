using System.Text.Json;
using PracticeX.Domain.Common;

namespace PracticeX.Domain.Evidence;

public sealed class EvidenceLink : Entity
{
    public Guid TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid DocumentAssetId { get; set; }
    public JsonDocument PageRefs { get; set; } = JsonDocument.Parse("[]");
    public string Quote { get; set; } = string.Empty;
    public Guid? SourceObjectId { get; set; }
    public Guid? AgentRunId { get; set; }
}

