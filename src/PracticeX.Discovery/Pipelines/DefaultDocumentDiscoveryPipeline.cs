using PracticeX.Discovery.Classification;
using PracticeX.Discovery.Contracts;
using PracticeX.Discovery.Validation;
using PracticeX.Domain.Documents;

namespace PracticeX.Discovery.Pipelines;

/// <summary>
/// Default impl. Today: classifier + validity inspector (when bytes available).
/// Slice 2 inserts signature detection into the same call path so HasSignature/
/// SignatureCount/SignatureProviders get populated. The cloud orchestrator and
/// the desktop agent both call into this — their wire output is byte-identical.
/// </summary>
public sealed class DefaultDocumentDiscoveryPipeline(
    IDocumentClassifier classifier,
    IDocumentValidityInspector? validityInspector = null) : IDocumentDiscoveryPipeline
{
    public Task<ManifestScoredItemDto> ScoreAsync(
        ManifestItemDto item,
        byte[]? content,
        CancellationToken cancellationToken)
    {
        var folderHint = ExtractFolderHint(item.RelativePath);

        var classification = classifier.Classify(new ClassificationInput
        {
            FileName = item.Name,
            RelativePath = item.RelativePath,
            MimeType = item.MimeType ?? "application/octet-stream",
            SizeBytes = item.SizeBytes,
            FolderHint = folderHint,
            Hints = []
        });

        var reasonCodes = classification.ReasonCodes.ToList();

        // Slice 2 will populate these inside this same method.
        var hasSignature = false;
        var signatureCount = 0;
        IReadOnlyList<string> signatureProviders = Array.Empty<string>();

        if (content is not null && content.Length > 0 && validityInspector is not null)
        {
            var validity = validityInspector.Inspect(content, item.MimeType ?? "application/octet-stream", item.Name);
            foreach (var rc in validity.ReasonCodes)
            {
                reasonCodes.Add(rc);
            }
        }

        var manifestItemId = BuildManifestItemId(item);
        var band = ManifestBands.From(classification.Confidence);
        var action = ManifestBands.RecommendedAction(classification.Confidence);

        var scored = new ManifestScoredItemDto(
            ManifestItemId: manifestItemId,
            RelativePath: item.RelativePath,
            Name: item.Name,
            SizeBytes: item.SizeBytes,
            CandidateType: classification.CandidateType,
            Confidence: classification.Confidence,
            ReasonCodes: reasonCodes,
            RecommendedAction: action,
            Band: band,
            CounterpartyHint: classification.CounterpartyHint,
            HasSignature: hasSignature,
            SignatureCount: signatureCount,
            SignatureProviders: signatureProviders
        );

        return Task.FromResult(scored);
    }

    public static string BuildManifestItemId(ManifestItemDto item) =>
        $"manifest:{item.RelativePath}|{item.SizeBytes}|{item.LastModifiedUtc.ToUnixTimeSeconds()}";

    private static string? ExtractFolderHint(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return null;
        }
        var normalized = relativePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash <= 0 ? null : normalized[..lastSlash];
    }
}

