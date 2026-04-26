using PracticeX.Domain.Common;

namespace PracticeX.Domain.Documents;

public sealed class DocumentAsset : Entity
{
    public Guid TenantId { get; set; }
    public Guid? SourceObjectId { get; set; }
    public string StorageUri { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? PageCount { get; set; }
    public string TextStatus { get; set; } = "pending";
    public string OcrStatus { get; set; } = "pending";
    public string? ExtractionRoute { get; set; }
    public string? ValidityStatus { get; set; }
    public bool? HasTextLayer { get; set; }
    public bool? IsEncrypted { get; set; }
}

public static class ExtractionRoutes
{
    public const string LocalText = "local_text";
    public const string OcrFirstPages = "ocr_first_pages";
    public const string FullOcr = "full_ocr";
    public const string Skip = "skip";
    public const string ManualReview = "manual_review";
}

public static class ValidityStatuses
{
    public const string Valid = "valid";
    public const string Encrypted = "encrypted";
    public const string Corrupt = "corrupt";
    public const string Unsupported = "unsupported";
    public const string Unknown = "unknown";
}

