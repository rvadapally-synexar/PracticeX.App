using System.IO.Compression;
using PracticeX.Application.SourceDiscovery.Validation;
using PracticeX.Domain.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace PracticeX.Infrastructure.SourceDiscovery.Validation;

/// <summary>
/// Cheap server-side container check that runs after bytes land. Drives
/// extraction_route + validity_status + has_text_layer + is_encrypted on
/// document_assets so downstream extraction can be routed without touching
/// content-AI providers.
/// </summary>
public sealed class BasicDocumentValidityInspector : IDocumentValidityInspector
{
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46]; // %PDF
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04]; // PK\x03\x04

    public ValidityReport Inspect(byte[] content, string mimeType, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(mimeType);
        ArgumentNullException.ThrowIfNull(fileName);

        if (content.Length == 0)
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Unsupported,
                ExtractionRoute = ExtractionRoutes.Skip,
                ReasonCodes = ["empty_file"]
            };
        }

        var normalizedMime = mimeType.ToLowerInvariant();
        if (normalizedMime.Contains("pdf") || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return InspectPdf(content);
        }

        if (IsOfficeZipMime(normalizedMime) || IsOfficeZipExt(fileName))
        {
            return InspectOfficeZip(content);
        }

        if (normalizedMime.StartsWith("text/") || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Valid,
                ExtractionRoute = ExtractionRoutes.LocalText,
                HasTextLayer = true,
                IsEncrypted = false,
                ReasonCodes = ["valid_text"]
            };
        }

        return new ValidityReport
        {
            ValidityStatus = ValidityStatuses.Unsupported,
            ExtractionRoute = ExtractionRoutes.Skip,
            ReasonCodes = ["unsupported_container"]
        };
    }

    private static ValidityReport InspectPdf(byte[] content)
    {
        if (!StartsWith(content, PdfSignature))
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Corrupt,
                ExtractionRoute = ExtractionRoutes.Skip,
                ReasonCodes = ["corrupt_pdf"]
            };
        }

        try
        {
            using var doc = PdfDocument.Open(content);
            var pageCount = doc.NumberOfPages;
            var hasTextLayer = HasAnyTextLayer(doc);

            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Valid,
                PageCount = pageCount,
                HasTextLayer = hasTextLayer,
                IsEncrypted = false,
                ExtractionRoute = hasTextLayer
                    ? ExtractionRoutes.LocalText
                    : ExtractionRoutes.OcrFirstPages,
                ReasonCodes = hasTextLayer
                    ? ["valid_pdf", "pdf_has_text_layer"]
                    : ["valid_pdf", "pdf_scanned"]
            };
        }
        catch (PdfDocumentEncryptedException)
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Encrypted,
                IsEncrypted = true,
                ExtractionRoute = ExtractionRoutes.ManualReview,
                ReasonCodes = ["encrypted_pdf"]
            };
        }
        catch (Exception)
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Corrupt,
                ExtractionRoute = ExtractionRoutes.Skip,
                ReasonCodes = ["corrupt_pdf"]
            };
        }
    }

    private static ValidityReport InspectOfficeZip(byte[] content)
    {
        if (!StartsWith(content, ZipSignature))
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Corrupt,
                ExtractionRoute = ExtractionRoutes.Skip,
                ReasonCodes = ["corrupt_office_container"]
            };
        }

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var hasContentTypes = archive.Entries.Any(e => e.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase));
            if (!hasContentTypes)
            {
                return new ValidityReport
                {
                    ValidityStatus = ValidityStatuses.Corrupt,
                    ExtractionRoute = ExtractionRoutes.Skip,
                    ReasonCodes = ["corrupt_office_container"]
                };
            }

            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Valid,
                HasTextLayer = true,
                IsEncrypted = false,
                ExtractionRoute = ExtractionRoutes.LocalText,
                ReasonCodes = ["valid_office_container"]
            };
        }
        catch (Exception)
        {
            return new ValidityReport
            {
                ValidityStatus = ValidityStatuses.Corrupt,
                ExtractionRoute = ExtractionRoutes.Skip,
                ReasonCodes = ["corrupt_office_container"]
            };
        }
    }

    private static bool HasAnyTextLayer(PdfDocument doc)
    {
        var pagesToProbe = Math.Min(doc.NumberOfPages, 3);
        for (var i = 1; i <= pagesToProbe; i++)
        {
            try
            {
                var page = doc.GetPage(i);
                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    return true;
                }
            }
            catch
            {
                // Best effort: a single bad page doesn't change overall verdict.
            }
        }
        return false;
    }

    private static bool IsOfficeZipMime(string mime) =>
        mime.Contains("officedocument") ||
        mime.Contains("ms-excel") ||
        mime.Contains("msword");

    private static bool IsOfficeZipExt(string name) =>
        name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);

    private static bool StartsWith(byte[] content, byte[] signature)
    {
        if (content.Length < signature.Length)
        {
            return false;
        }
        for (var i = 0; i < signature.Length; i++)
        {
            if (content[i] != signature[i])
            {
                return false;
            }
        }
        return true;
    }
}
