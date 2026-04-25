using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class DocumentCandidate : Entity
{
    public Guid TenantId { get; set; }
    public Guid DocumentAssetId { get; set; }
    public string CandidateType { get; set; } = DocumentCandidateTypes.Unknown;
    public Guid? FacilityHintId { get; set; }
    public string? CounterpartyHint { get; set; }
    public decimal Confidence { get; set; }
    public string Status { get; set; } = DocumentCandidateStatus.Candidate;

    public string? ReasonCodesJson { get; set; }
    public string ClassifierVersion { get; set; } = "rule_v1";
    public string? OriginFilename { get; set; }
    public string? RelativePath { get; set; }
    public Guid? SourceObjectId { get; set; }
}

public static class DocumentCandidateTypes
{
    public const string Unknown = "unknown";
    public const string PayerContract = "payer_contract";
    public const string VendorContract = "vendor_contract";
    public const string Lease = "lease";
    public const string EmployeeAgreement = "employee_agreement";
    public const string ProcessorAgreement = "processor_agreement";
    public const string Amendment = "amendment";
    public const string FeeSchedule = "fee_schedule";
    public const string Other = "other";
}

public static class DocumentCandidateStatus
{
    public const string Candidate = "candidate";
    public const string Skipped = "skipped";
    public const string PendingReview = "pending_review";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
}
