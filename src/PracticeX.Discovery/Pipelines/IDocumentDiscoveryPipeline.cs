using PracticeX.Discovery.Contracts;

namespace PracticeX.Discovery.Pipelines;

/// <summary>
/// Single entry point for content-aware scoring of a manifest item. Identical
/// behaviour whether called from the cloud /folder/manifest endpoint or the
/// desktop agent's --mode local-prefilter path. Produces a wire-shape
/// ManifestScoredItemDto so transports just serialise the result.
///
/// Bytes are optional. When null, scoring uses metadata only (filename, size,
/// path, mime). When provided, the pipeline also runs the validity inspector
/// and (in Slice 2+) signature detection against the byte content.
/// </summary>
public interface IDocumentDiscoveryPipeline
{
    Task<ManifestScoredItemDto> ScoreAsync(
        ManifestItemDto item,
        byte[]? content,
        CancellationToken cancellationToken);
}
