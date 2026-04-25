using PracticeX.Application.Common;

namespace PracticeX.Application.SourceDiscovery.Connectors;

/// <summary>
/// All connectors implement this. They describe themselves and produce discovery
/// results. They never silently mutate canonical contract records — they only emit
/// candidate items consumed by the ingestion orchestrator.
/// </summary>
public interface ISourceConnector
{
    string SourceType { get; }
    ConnectorDescriptor Describe();
    Task<Result<DiscoveryResult>> DiscoverAsync(DiscoveryRequest request, CancellationToken cancellationToken);
}

public interface IConnectorRegistry
{
    IReadOnlyCollection<ConnectorDescriptor> Describe();
    ISourceConnector? Resolve(string sourceType);
}
