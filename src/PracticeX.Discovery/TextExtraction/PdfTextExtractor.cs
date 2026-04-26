using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace PracticeX.Discovery.TextExtraction;

/// <summary>
/// PDF text extractor backed by PdfPig 0.1.14. Walks pages 1..N (or 1..maxPages
/// when set) and assembles per-page <see cref="ExtractedPage"/> records plus a
/// <c>FullText</c> blob. PdfPig 0.1.14 doesn't expose structure tags, so the
/// headings list stays empty — the LLM stage will infer headings later.
///
/// Defensive by design: encrypted PDFs and parser blow-ups return
/// <see cref="TextExtractionResult.Empty"/> with <c>Notes</c> populated rather
/// than throwing. Same instance is reused across requests (no per-call state).
/// </summary>
public sealed class PdfTextExtractor : IDocumentTextExtractor
{
    public string Name => "pdf-text";

    public bool CanExtract(string mimeType, string fileName)
    {
        var mime = mimeType?.ToLowerInvariant() ?? string.Empty;
        return mime.Contains("pdf") || (fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public TextExtractionResult Extract(byte[] content, string mimeType, string fileName, int? maxPages = null)
    {
        if (content is null || content.Length == 0)
        {
            return TextExtractionResult.Empty with { ExtractorName = Name, Notes = "empty" };
        }

        try
        {
            using var doc = PdfDocument.Open(content);

            var totalPages = doc.NumberOfPages;
            var limit = maxPages.HasValue ? Math.Min(totalPages, Math.Max(0, maxPages.Value)) : totalPages;
            var truncated = maxPages.HasValue && totalPages > limit;

            var pages = new List<ExtractedPage>(limit);
            for (var i = 1; i <= limit; i++)
            {
                try
                {
                    var page = doc.GetPage(i);
                    pages.Add(new ExtractedPage(i, page.Text ?? string.Empty));
                }
                catch
                {
                    // Skip a page that won't render rather than failing the whole doc.
                    pages.Add(new ExtractedPage(i, string.Empty));
                }
            }

            var fullText = string.Join("\n\n", pages.Select(p => p.Text));

            return new TextExtractionResult
            {
                FullText = fullText,
                Pages = pages,
                Headings = [],
                ExtractorName = Name,
                Truncated = truncated
            };
        }
        catch (PdfDocumentEncryptedException)
        {
            return TextExtractionResult.Empty with { ExtractorName = Name, Notes = "encrypted" };
        }
        catch (Exception ex)
        {
            return TextExtractionResult.Empty with { ExtractorName = Name, Notes = ex.Message };
        }
    }
}
