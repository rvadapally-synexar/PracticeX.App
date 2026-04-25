namespace PracticeX.Application.SourceDiscovery.Outlook;

public interface IMicrosoftGraphOAuthService
{
    /// <summary>
    /// Build the Microsoft identity platform authorization URL for read-only mailbox scope.
    /// State must round-trip through callback so we can match it to the source connection.
    /// </summary>
    string BuildAuthorizationUrl(string state, string redirectUri);

    /// <summary>
    /// Exchange the authorization code for access + refresh tokens. Refresh token is
    /// persisted server-side via IMicrosoftGraphTokenStore; this method does not
    /// expose tokens to callers.
    /// </summary>
    Task<TokenAcquisitionResult> AcquireTokenAsync(string authorizationCode, string redirectUri, CancellationToken cancellationToken);

    /// <summary>
    /// Refresh an existing token using the stored refresh token. Returns a fresh
    /// access token with TTL.
    /// </summary>
    Task<AccessTokenLease> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// True when client_id, tenant_id, and client_secret are configured. False in
    /// fully-local demo mode where the OAuth service is stubbed.
    /// </summary>
    bool IsConfigured { get; }
}

public sealed record TokenAcquisitionResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string Subject,
    string Scope
);

public sealed record AccessTokenLease(string AccessToken, DateTimeOffset ExpiresAt);
