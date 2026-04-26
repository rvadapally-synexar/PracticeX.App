using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Discovery.Validation;
using PracticeX.Domain.Documents;

namespace PracticeX.Infrastructure.SourceDiscovery.Complexity;

/// <summary>
/// Tiering rules for DOCX (per plan A6):
///   S: no macros, no tracked changes, no comments, no embedded objects
///   M: tracked changes OR comments OR signature lines
///   L: ≥5 embedded objects OR > 100 pages estimate
///   X: macros (.docm) OR password-protected
/// </summary>
public sealed class DocxComplexityProfiler
{
    private const long OversizeBytes = 100L * 1024 * 1024;
    private const int CharsPerPageEstimate = 1800;

    public ComplexityReport Profile(byte[] content, string fileName, ValidityReport validity)
    {
        var factors = new List<string>();
        var blockers = new List<string>();
        var sizeBytes = content.LongLength;

        if (sizeBytes > OversizeBytes)
        {
            factors.Add(ComplexityFactors.OversizeFile);
            blockers.Add(ComplexityBlockers.OversizeFile);
        }

        var macroByExtension = fileName.EndsWith(".docm", StringComparison.OrdinalIgnoreCase);
        if (macroByExtension)
        {
            blockers.Add(ComplexityBlockers.MacrosDetected);
        }

        if (validity.IsEncrypted == true || validity.ValidityStatus == ValidityStatuses.Encrypted)
        {
            blockers.Add(ComplexityBlockers.PasswordProtected);
            return Report(ComplexityTier.Extra, factors, blockers,
                BuildMetadata(sizeBytes, 0, 0, 0, 0, 0, macroByExtension));
        }

        var trackedChangeCount = 0;
        var commentCount = 0;
        var signatureLineCount = 0;
        var embeddedObjectCount = 0;
        var pageEstimate = 0;
        var hasMacros = macroByExtension;

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var doc = WordprocessingDocument.Open(stream, isEditable: false);

            if (doc.MainDocumentPart?.VbaProjectPart is not null)
            {
                hasMacros = true;
                if (!blockers.Contains(ComplexityBlockers.MacrosDetected))
                {
                    blockers.Add(ComplexityBlockers.MacrosDetected);
                }
            }

            var body = doc.MainDocumentPart?.Document.Body;
            if (body is not null)
            {
                trackedChangeCount =
                    body.Descendants<InsertedRun>().Count() +
                    body.Descendants<DeletedRun>().Count() +
                    body.Descendants<ParagraphPropertiesChange>().Count();

                commentCount = doc.MainDocumentPart?.WordprocessingCommentsPart?.Comments?.Elements<Comment>().Count() ?? 0;

                // Signature line content controls — scan SDT blocks for the signature pattern.
                signatureLineCount = body.Descendants<SdtElement>()
                    .Count(sdt => sdt.InnerXml?.Contains("signatureLine", StringComparison.OrdinalIgnoreCase) == true);

                embeddedObjectCount =
                    body.Descendants<EmbeddedObject>().Count() +
                    (doc.MainDocumentPart?.EmbeddedObjectParts.Count() ?? 0) +
                    (doc.MainDocumentPart?.EmbeddedPackageParts.Count() ?? 0);

                // Page estimate: char count / 1800 (typical letter-size page)
                var charCount = body.Descendants<Text>().Sum(t => t.Text?.Length ?? 0);
                pageEstimate = Math.Max(1, charCount / CharsPerPageEstimate);
            }
        }
        catch
        {
            return Report(ComplexityTier.Moderate, factors, blockers,
                BuildMetadata(sizeBytes, 0, 0, 0, 0, 0, hasMacros));
        }

        if (trackedChangeCount > 0) factors.Add(ComplexityFactors.TrackedChanges);
        if (commentCount > 0) factors.Add(ComplexityFactors.Comments);
        if (signatureLineCount > 0) factors.Add(ComplexityFactors.SignatureLines);
        if (embeddedObjectCount > 0) factors.Add(ComplexityFactors.EmbeddedObjects);
        if (pageEstimate > 100) factors.Add(ComplexityFactors.LongDocument);

        var tier = AssignTier(hasMacros, trackedChangeCount, commentCount, signatureLineCount,
            embeddedObjectCount, pageEstimate, sizeBytes);

        return Report(tier, factors, blockers,
            BuildMetadata(sizeBytes, trackedChangeCount, commentCount, signatureLineCount,
                embeddedObjectCount, pageEstimate, hasMacros));
    }

    private static ComplexityTier AssignTier(
        bool hasMacros, int trackedChanges, int comments, int signatureLines,
        int embeddedObjects, int pageEstimate, long sizeBytes)
    {
        if (hasMacros || sizeBytes > OversizeBytes) return ComplexityTier.Extra;
        if (embeddedObjects >= 5 || pageEstimate > 100) return ComplexityTier.Large;
        if (trackedChanges > 0 || comments > 0 || signatureLines > 0) return ComplexityTier.Moderate;
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

    private static string BuildMetadata(
        long sizeBytes, int trackedChangeCount, int commentCount, int signatureLineCount,
        int embeddedObjectCount, int pageEstimate, bool hasMacros)
        => JsonSerializer.Serialize(new
        {
            format = "docx",
            sizeBytes,
            pageEstimate,
            trackedChangeCount,
            commentCount,
            signatureLineCount,
            embeddedObjectCount,
            hasMacros
        });
}
