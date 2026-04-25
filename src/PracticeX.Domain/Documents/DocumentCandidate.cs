using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class DocumentCandidate : Entity
{
    public Guid TenantId { get; set; }
    public Guid DocumentAssetId { get; set; }
    public string CandidateType { get; set; } = "unknown";
    public Guid? FacilityHintId { get; set; }
    public string? CounterpartyHint { get; set; }
    public decimal Confidence { get; set; }
    public string Status { get; set; } = "candidate";
}

