using PracticeX.Discovery.TextExtraction;

namespace PracticeX.Discovery.FieldExtraction;

/// <summary>
/// Pulls structured fields out of a contract once the classifier has named
/// the family. One extractor per contract family (Employment, NDA, Corporate);
/// the orchestrator picks the matching one by candidate type / subtype.
///
/// Pure logic — no DI, no DB, no HTTP. Same impl runs in the cloud
/// orchestrator and (later) the desktop agent's local-prefilter mode.
/// </summary>
public interface IContractFieldExtractor
{
    /// <summary>Stable name used in logs and audit trails.</summary>
    string Name { get; }

    /// <summary>Wire-level schema version this extractor produces, e.g. "employment_v1".</summary>
    string SchemaVersion { get; }

    /// <summary>Returns true when this extractor handles the given subtype or classifier candidate type.</summary>
    bool CanExtract(string subtypeOrCandidateType);

    FieldExtractionResult Extract(FieldExtractionInput input);
}

public sealed record FieldExtractionInput
{
    public required string FullText { get; init; }
    public required IReadOnlyList<ExtractedPage> Pages { get; init; }
    public IReadOnlyList<ExtractedHeading> Headings { get; init; } = [];
    public required string FileName { get; init; }
    public string? CandidateType { get; init; }
    public string? SignatureProvider { get; init; }
    public string? DocusignEnvelopeId { get; init; }
}

public sealed record FieldExtractionResult
{
    public required string SchemaVersion { get; init; }
    public required string Subtype { get; init; }
    public required IReadOnlyDictionary<string, ExtractedField> Fields { get; init; }
    public bool IsTemplate { get; init; }
    public bool IsExecuted { get; init; }
    public required IReadOnlyList<string> ReasonCodes { get; init; }
    public string? Notes { get; init; }
}

public sealed record ExtractedField
{
    public required string Name { get; init; }
    public required object? Value { get; init; }
    public decimal Confidence { get; init; }
    public string? SourceCitation { get; init; }
}
