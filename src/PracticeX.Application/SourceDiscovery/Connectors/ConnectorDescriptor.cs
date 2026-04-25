namespace PracticeX.Application.SourceDiscovery.Connectors;

/// <summary>
/// Static, UI-facing description of a connector. Returned from the connector registry
/// so the UI can render available connectors without hardcoding their names.
/// </summary>
public sealed record ConnectorDescriptor(
    string SourceType,
    string DisplayName,
    string Summary,
    ConnectorAuthMode AuthMode,
    IReadOnlyCollection<string> SupportedMimeTypes,
    bool IsReadOnly,
    string Status
);

public enum ConnectorAuthMode
{
    None,
    OAuth,
    ApiKey
}
