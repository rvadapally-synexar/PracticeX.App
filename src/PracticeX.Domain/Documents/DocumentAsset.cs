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
}

