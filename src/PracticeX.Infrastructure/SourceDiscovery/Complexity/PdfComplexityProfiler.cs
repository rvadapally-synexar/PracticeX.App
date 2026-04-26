using System.Text.Json;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Discovery.Validation;
using PracticeX.Domain.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace PracticeX.Infrastructure.SourceDiscovery.Complexity;

/// <summary>
/// Tiering rules for PDF (per plan A5):
///   S: ≤10 pages AND text layer AND no form fields AND no embedded files
///   M: 11-50 pages OR form fields OR no text layer (needs OCR)
///   L: 51-500 pages OR embedded files OR JavaScript
///   X: encrypted OR > 500 pages OR > 100 MB
/// </summary>
public sealed class PdfComplexityProfiler
{
    private const long OversizeBytes = 100L * 1024 * 1024;

    public ComplexityReport Profile(byte[] content, ValidityReport validity)
    {
        var factors = new List<string>();
        var blockers = new List<string>();

        var sizeBytes = content.LongLength;
        var oversize = sizeBytes > OversizeBytes;
        if (oversize)
        {
            factors.Add(ComplexityFactors.LargePdf);
            blockers.Add(ComplexityBlockers.OversizeFile);
        }

        if (validity.IsEncrypted == true || validity.ValidityStatus == ValidityStatuses.Encrypted)
        {
            blockers.Add(ComplexityBlockers.PasswordProtected);
            return Report(ComplexityTier.Extra, factors, blockers, BuildMetadata(validity, hasFormFields: false, hasEmbeddedFiles: false, hasJavaScript: false, sizeBytes));
        }

        if (validity.ValidityStatus is ValidityStatuses.Corrupt or ValidityStatuses.Unsupported)
        {
            // Not really our problem to tier — caller already saw the validity error.
            return Report(ComplexityTier.Simple, factors, blockers, BuildMetadata(validity, false, false, false, sizeBytes));
        }

        var hasFormFields = false;
        var hasEmbeddedFiles = false;
        var hasJavaScript = false;
        var pageCount = validity.PageCount ?? 0;
        var hasTextLayer = validity.HasTextLayer == true;

        try
        {
            using var doc = PdfDocument.Open(content);
            pageCount = doc.NumberOfPages;
            // Form-field / embedded-file detection via PdfPig is brittle across
            // versions; deferred until we wire the Discovery library's signature
            // detector which already opens the catalog.
        }
        catch (PdfDocumentEncryptedException)
        {
            blockers.Add(ComplexityBlockers.PasswordProtected);
            return Report(ComplexityTier.Extra, factors, blockers, BuildMetadata(validity, false, false, false, sizeBytes));
        }
        catch
        {
            // Validity inspector already flagged corruption; treat as Simple for tiering purposes.
        }

        if (!hasTextLayer)
        {
            factors.Add(ComplexityFactors.ScannedPdf);
        }
        if (hasFormFields) factors.Add(ComplexityFactors.FormFields);
        if (hasEmbeddedFiles) factors.Add(ComplexityFactors.EmbeddedFiles);
        if (hasJavaScript) factors.Add(ComplexityFactors.PdfJavaScript);

        if (pageCount > 500) factors.Add(ComplexityFactors.ManyPages);
        else if (pageCount > 50) factors.Add(ComplexityFactors.ManyPages);

        var tier = AssignTier(pageCount, hasTextLayer, hasFormFields, hasEmbeddedFiles, hasJavaScript, oversize);
        return Report(tier, factors, blockers, BuildMetadata(validity, hasFormFields, hasEmbeddedFiles, hasJavaScript, sizeBytes, pageCount));
    }

    private static ComplexityTier AssignTier(
        int pageCount,
        bool hasTextLayer,
        bool hasFormFields,
        bool hasEmbeddedFiles,
        bool hasJavaScript,
        bool oversize)
    {
        if (oversize || pageCount > 500) return ComplexityTier.Extra;
        if (pageCount > 50 || hasEmbeddedFiles || hasJavaScript) return ComplexityTier.Large;
        if (pageCount > 10 || hasFormFields || !hasTextLayer) return ComplexityTier.Moderate;
        return ComplexityTier.Simple;
    }

    private static ComplexityReport Report(ComplexityTier tier, List<string> factors, List<string> blockers, string metadataJson)
        => new()
        {
            Tier = tier,
            Factors = factors,
            Blockers = blockers,
            MetadataJson = metadataJson
        };

    private static string BuildMetadata(ValidityReport validity, bool hasFormFields, bool hasEmbeddedFiles, bool hasJavaScript, long sizeBytes, int? pageCountOverride = null)
        => JsonSerializer.Serialize(new
        {
            format = "pdf",
            sizeBytes,
            pageCount = pageCountOverride ?? validity.PageCount,
            hasTextLayer = validity.HasTextLayer,
            isEncrypted = validity.IsEncrypted,
            hasFormFields,
            hasEmbeddedFiles,
            hasJavaScript
        });
}
