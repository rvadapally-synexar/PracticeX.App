using System.Text.Json;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Discovery.Validation;

namespace PracticeX.Infrastructure.SourceDiscovery.Complexity;

/// <summary>
/// Single entry-point that the orchestrator calls. Dispatches by mime/extension
/// to the right per-format profiler. Always returns a report — unknown formats
/// land in Simple with empty factors so downstream code never has to null-check.
/// </summary>
public sealed class CompositeComplexityProfiler : IComplexityProfiler
{
    private readonly PdfComplexityProfiler _pdf;
    private readonly ExcelComplexityProfiler _excel;
    private readonly DocxComplexityProfiler _docx;
    private readonly PlainTextComplexityProfiler _text;

    public CompositeComplexityProfiler(
        PdfComplexityProfiler pdf,
        ExcelComplexityProfiler excel,
        DocxComplexityProfiler docx,
        PlainTextComplexityProfiler text)
    {
        _pdf = pdf;
        _excel = excel;
        _docx = docx;
        _text = text;
    }

    public ComplexityReport Profile(byte[] content, string mimeType, string fileName, ValidityReport validity)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(mimeType);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(validity);

        if (content.Length == 0)
        {
            return ComplexityReport.Empty();
        }

        var mime = mimeType.ToLowerInvariant();
        var name = fileName.ToLowerInvariant();

        if (mime.Contains("pdf") || name.EndsWith(".pdf"))
        {
            return _pdf.Profile(content, validity);
        }

        if (IsExcelMime(mime) || name.EndsWith(".xlsx") || name.EndsWith(".xlsm"))
        {
            return _excel.Profile(content, fileName, validity);
        }

        if (IsWordMime(mime) || name.EndsWith(".docx") || name.EndsWith(".docm"))
        {
            return _docx.Profile(content, fileName, validity);
        }

        if (mime.StartsWith("text/") || name.EndsWith(".txt") || name.EndsWith(".csv"))
        {
            return _text.Profile(content, mimeType, fileName, validity);
        }

        return new ComplexityReport
        {
            Tier = ComplexityTier.Simple,
            Factors = Array.Empty<string>(),
            Blockers = Array.Empty<string>(),
            MetadataJson = JsonSerializer.Serialize(new { format = "unknown", sizeBytes = content.LongLength, mimeType })
        };
    }

    private static bool IsExcelMime(string mime) =>
        mime.Contains("spreadsheetml") || mime.Contains("ms-excel") || mime.Contains("excel");

    private static bool IsWordMime(string mime) =>
        mime.Contains("wordprocessingml") || mime.Contains("msword");
}
