namespace PracticeX.Discovery.TextExtraction;

/// <summary>
/// Default <see cref="IDocumentTextExtractor"/> — dispatches to the first
/// registered extractor whose <c>CanExtract</c> returns true. Mirrors the
/// composite signature detector pattern: filter self out of the chain to
/// avoid recursion when DI registers this type as both the singleton and a
/// member of the enumerable.
/// </summary>
public sealed class CompositeDocumentTextExtractor(IEnumerable<IDocumentTextExtractor> extractors) : IDocumentTextExtractor
{
    private readonly IReadOnlyList<IDocumentTextExtractor> _extractors =
        extractors.Where(e => e is not CompositeDocumentTextExtractor).ToList();

    public string Name => "composite";

    public bool CanExtract(string mimeType, string fileName) =>
        _extractors.Any(e => e.CanExtract(mimeType, fileName));

    public TextExtractionResult Extract(byte[] content, string mimeType, string fileName, int? maxPages = null)
    {
        if (_extractors.Count == 0 || content is null || content.Length == 0)
        {
            return TextExtractionResult.Empty;
        }

        foreach (var extractor in _extractors)
        {
            if (!extractor.CanExtract(mimeType, fileName)) continue;
            return extractor.Extract(content, mimeType, fileName, maxPages);
        }

        return TextExtractionResult.Empty;
    }
}
