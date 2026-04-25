using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class IngestionJob : Entity
{
    public Guid TenantId { get; set; }
    public Guid BatchId { get; set; }
    public Guid? SourceObjectId { get; set; }
    public Guid? DocumentAssetId { get; set; }
    public string Status { get; set; } = IngestionJobStatus.Queued;
    public string Stage { get; set; } = IngestionStage.Discovered;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
}

public static class IngestionJobStatus
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Skipped = "skipped";
    public const string Failed = "failed";
}

public static class IngestionStage
{
    public const string Discovered = "discovered";
    public const string Stored = "stored";
    public const string Hashed = "hashed";
    public const string Classified = "classified";
    public const string ReviewQueued = "review_queued";
}

public static class IngestionReasonCodes
{
    public const string UnsupportedMimeType = "unsupported_mime_type";
    public const string DuplicateContent = "duplicate_content";
    public const string EmptyFile = "empty_file";
    public const string ExceedsSizeLimit = "exceeds_size_limit";
    public const string LikelyContract = "likely_contract";
    public const string AmbiguousType = "ambiguous_type";
    public const string FilenameContractKeywords = "filename_contract_keywords";
    public const string FilenameAmendment = "filename_amendment";
    public const string FilenameRateSchedule = "filename_rate_schedule";
    public const string FolderHintPayer = "folder_hint_payer";
    public const string FolderHintLease = "folder_hint_lease";
    public const string OutlookSenderDomain = "outlook_sender_domain";
    public const string OutlookSubjectKeywords = "outlook_subject_keywords";
}
