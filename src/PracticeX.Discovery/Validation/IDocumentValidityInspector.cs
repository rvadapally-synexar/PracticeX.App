namespace PracticeX.Discovery.Validation;

public interface IDocumentValidityInspector
{
    ValidityReport Inspect(byte[] content, string mimeType, string fileName);
}

public sealed record ValidityReport
{
    public required string ValidityStatus { get; init; }
    public int? PageCount { get; init; }
    public bool? HasTextLayer { get; init; }
    public bool? IsEncrypted { get; init; }
    public string ExtractionRoute { get; init; } = "manual_review";
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
}
