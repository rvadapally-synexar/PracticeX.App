namespace PracticeX.Discovery.Contracts;

public sealed record ConnectorDescriptorDto(
    string SourceType,
    string DisplayName,
    string Summary,
    string AuthMode,
    bool IsReadOnly,
    string Status,
    IReadOnlyCollection<string> SupportedMimeTypes
);

public sealed record SourceConnectionDto(
    Guid Id,
    string SourceType,
    string Status,
    string? DisplayName,
    string? OauthSubject,
    DateTimeOffset? LastSyncAt,
    DateTimeOffset CreatedAt,
    string? LastError
);

public sealed record CreateConnectionRequest(string SourceType, string? DisplayName);
