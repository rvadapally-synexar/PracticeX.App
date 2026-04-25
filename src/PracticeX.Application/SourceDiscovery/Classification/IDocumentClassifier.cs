namespace PracticeX.Application.SourceDiscovery.Classification;

public interface IDocumentClassifier
{
    /// <summary>
    /// Quick pre-ingestion classification used to populate the document candidate.
    /// Real per-document extraction happens later; this is the lightweight signal
    /// surfaced in the discovery UI.
    /// </summary>
    ClassificationResult Classify(ClassificationInput input);

    string Version { get; }
}

public sealed class ClassificationInput
{
    public required string FileName { get; init; }
    public string? RelativePath { get; init; }
    public required string MimeType { get; init; }
    public long? SizeBytes { get; init; }
    public string? SubjectHint { get; init; }
    public string? SenderHint { get; init; }
    public string? FolderHint { get; init; }
    public IReadOnlyList<string> Hints { get; init; } = [];
}

public sealed class ClassificationResult
{
    public required string CandidateType { get; init; }
    public required decimal Confidence { get; init; }
    public required IReadOnlyList<string> ReasonCodes { get; init; }
    public string Status { get; init; } = "candidate";
    public string? CounterpartyHint { get; init; }
}
