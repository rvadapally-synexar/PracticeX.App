using PracticeX.Application.SourceDiscovery.Connectors;

namespace PracticeX.Infrastructure.SourceDiscovery.Connectors;

public sealed class ConnectorRegistry(IEnumerable<ISourceConnector> connectors) : IConnectorRegistry
{
    private readonly Dictionary<string, ISourceConnector> _bySourceType =
        connectors.ToDictionary(c => c.SourceType, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ConnectorDescriptor> Describe()
        => _bySourceType.Values.Select(c => c.Describe()).ToList();

    public ISourceConnector? Resolve(string sourceType)
        => _bySourceType.TryGetValue(sourceType, out var connector) ? connector : null;
}
