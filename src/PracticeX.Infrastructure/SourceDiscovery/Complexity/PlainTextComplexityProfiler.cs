using System.Text.Json;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Discovery.Validation;

namespace PracticeX.Infrastructure.SourceDiscovery.Complexity;

/// <summary>
/// Trivial profiler. Plain text and CSV are always Simple unless oversize.
/// CSV-shaped XLSX gets routed here too via the composite dispatcher.
/// </summary>
public sealed class PlainTextComplexityProfiler
{
    private const long OversizeBytes = 50L * 1024 * 1024;

    public ComplexityReport Profile(byte[] content, string mimeType, string fileName, ValidityReport validity)
    {
        var sizeBytes = content.LongLength;
        var oversize = sizeBytes > OversizeBytes;

        var factors = oversize ? new List<string> { ComplexityFactors.OversizeFile } : new List<string>();
        var blockers = oversize ? new List<string> { ComplexityBlockers.OversizeFile } : new List<string>();

        return new ComplexityReport
        {
            Tier = oversize ? ComplexityTier.Large : ComplexityTier.Simple,
            Factors = factors,
            Blockers = blockers,
            MetadataJson = JsonSerializer.Serialize(new { format = "text", sizeBytes, mimeType })
        };
    }
}
