namespace PracticeX.Discovery.DocumentAi;

/// <summary>
/// Provider abstraction for OCR + structural extraction. Default cloud impl is
/// AzureDocumentIntelligenceProvider (lands in a later slice). Routing decisions
/// (local-text vs OCR vs custom-trained model) live in
/// IExtractionRouteDecider, separately, so providers stay dumb.
/// </summary>
public interface IDocumentIntelligenceProvider
{
    string Name { get; }
    bool IsConfigured { get; }

    /// <summary>
    /// Layout extraction: text + page structure + tables + signature regions.
    /// Used when validity inspector says the file needs OCR (no text layer).
    /// </summary>
    Task<DocumentExtractionResult> ExtractLayoutAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Field extraction against a custom-trained model (e.g. "synexar-nda-v1").
    /// Used when we have a model trained on samples for this contract type.
    /// </summary>
    Task<DocumentExtractionResult> ExtractFieldsAsync(
        DocumentExtractionRequest request,
        string modelId,
        CancellationToken cancellationToken);
}

public sealed record DocumentExtractionRequest(
    byte[] Content,
    string FileName,
    string MimeType,
    int? MaxPages = null
);

public sealed record DocumentExtractionResult(
    string FullText,
    IReadOnlyList<DocumentPage> Pages,
    IReadOnlyList<DocumentTable> Tables,
    IReadOnlyList<DocumentKeyValuePair> KeyValuePairs,
    IReadOnlyList<DocumentSignatureRegion> Signatures,
    string ProviderName,
    string ProviderModel,
    int TokensIn = 0,
    int TokensOut = 0,
    long LatencyMs = 0
);

public sealed record DocumentPage(
    int PageNumber,
    string Text,
    double Width,
    double Height
);

public sealed record DocumentTable(
    int PageNumber,
    int RowCount,
    int ColumnCount,
    IReadOnlyList<DocumentTableCell> Cells
);

public sealed record DocumentTableCell(
    int RowIndex,
    int ColumnIndex,
    string Text,
    double Confidence
);

public sealed record DocumentKeyValuePair(
    string Key,
    string Value,
    int? PageNumber,
    double Confidence
);

public sealed record DocumentSignatureRegion(
    int PageNumber,
    string? Provider,
    string? SignerName,
    DateTimeOffset? SignedAt,
    double Confidence
);
