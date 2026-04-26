using PracticeX.Discovery.Validation;

namespace PracticeX.Application.SourceDiscovery.Complexity;

/// <summary>
/// Inspects file bytes and returns a tier + factors describing how much work
/// the file will be to ingest. Runs after IDocumentValidityInspector and reuses
/// its report so we don't re-parse the container.
///
/// Implementations are deterministic and side-effect-free. Callers pass the
/// validity report so corrupt/encrypted files short-circuit cheaply.
/// </summary>
public interface IComplexityProfiler
{
    ComplexityReport Profile(byte[] content, string mimeType, string fileName, ValidityReport validity);
}
