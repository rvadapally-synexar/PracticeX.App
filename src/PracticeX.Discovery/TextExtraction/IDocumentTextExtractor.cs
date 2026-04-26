namespace PracticeX.Discovery.TextExtraction;

/// <summary>
/// Pulls plain text out of a document so downstream stages (LLM, regex
/// extractors, schema fillers) don't each have to re-open the file. One
/// extractor per format; the composite walks the chain and returns the
/// first match. Pure logic, no DI, no DB — same impl runs in the cloud
/// orchestrator and (later) the desktop agent's local-prefilter mode.
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>Stable name used in logs and audit trails.</summary>
    string Name { get; }

    /// <summary>Returns false when this extractor doesn't handle this mime/extension.</summary>
    bool CanExtract(string mimeType, string fileName);

    /// <summary>
    /// Extracts text. <paramref name="maxPages"/> caps work for paged formats
    /// (PDF) — when supplied and reached, the result has <c>Truncated = true</c>.
    /// Implementations must not throw — return <see cref="TextExtractionResult.Empty"/>
    /// with <c>Notes</c> populated on failure.
    /// </summary>
    TextExtractionResult Extract(byte[] content, string mimeType, string fileName, int? maxPages = null);
}

public sealed record TextExtractionResult
{
    /// <summary>Concatenated text across all pages — paragraphs separated by blank lines.</summary>
    public required string FullText { get; init; }

    /// <summary>Per-page slices. Single entry for non-paged formats (DOCX).</summary>
    public required IReadOnlyList<ExtractedPage> Pages { get; init; }

    /// <summary>Heading paragraphs detected by style. Empty for formats without structure.</summary>
    public IReadOnlyList<ExtractedHeading> Headings { get; init; } = [];

    /// <summary>Name of the extractor that produced this result. "none" if no match.</summary>
    public required string ExtractorName { get; init; }

    /// <summary>Free-form note (e.g. "encrypted", "not a zip") for diagnostics.</summary>
    public string? Notes { get; init; }

    /// <summary>True when extraction stopped early because <c>maxPages</c> was reached.</summary>
    public bool Truncated { get; init; }

    public static readonly TextExtractionResult Empty = new()
    {
        FullText = "",
        Pages = [],
        ExtractorName = "none"
    };
}

public sealed record ExtractedPage(int PageNumber, string Text);

public sealed record ExtractedHeading(string Text, int? PageNumber, int Level);
