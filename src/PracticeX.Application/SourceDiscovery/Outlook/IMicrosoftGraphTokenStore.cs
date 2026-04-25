namespace PracticeX.Application.SourceDiscovery.Outlook;

/// <summary>
/// Stores Microsoft Graph refresh tokens and short-lived access tokens for a source
/// connection. Production implementation backs onto Key Vault. The default
/// in-memory implementation is for local development only.
/// </summary>
public interface IMicrosoftGraphTokenStore
{
    Task SaveAsync(Guid sourceConnectionId, StoredGraphToken token, CancellationToken cancellationToken);
    Task<StoredGraphToken?> GetAsync(Guid sourceConnectionId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid sourceConnectionId, CancellationToken cancellationToken);
}

public sealed record StoredGraphToken(
    string Subject,
    string RefreshToken,
    string? CachedAccessToken,
    DateTimeOffset? CachedAccessTokenExpiresAt,
    string Scope
);
