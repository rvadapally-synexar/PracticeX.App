using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticeX.Application.SourceDiscovery.Outlook;

namespace PracticeX.Infrastructure.SourceDiscovery.Outlook;

/// <summary>
/// OAuth 2.0 authorization-code flow implementation against Microsoft identity
/// platform v2 endpoints. Reads client_id, client_secret and tenant_id from
/// configuration. Never logs secrets or full tokens.
/// </summary>
public sealed class MicrosoftGraphOAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<MicrosoftGraphOptions> options,
    ILogger<MicrosoftGraphOAuthService> logger) : IMicrosoftGraphOAuthService
{
    private const string AuthorizeUrlTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0/authorize";
    private const string TokenUrlTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

    private readonly MicrosoftGraphOptions _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ClientId) &&
        !string.IsNullOrWhiteSpace(_options.ClientSecret);

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Microsoft Graph is not configured. Set MicrosoftGraph:ClientId / ClientSecret.");
        }

        var scope = string.Join(' ', _options.Scopes);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["response_mode"] = "query",
            ["scope"] = scope,
            ["state"] = state,
            ["prompt"] = "select_account"
        };

        var url = string.Format(AuthorizeUrlTemplate, _options.TenantId);
        var qs = string.Join('&', query
            .Where(kvp => kvp.Value is not null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));
        return $"{url}?{qs}";
    }

    public async Task<TokenAcquisitionResult> AcquireTokenAsync(string authorizationCode, string redirectUri, CancellationToken cancellationToken)
    {
        var response = await PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["code"] = authorizationCode,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = string.Join(' ', _options.Scopes)
        }, cancellationToken);

        var subject = ExtractIdTokenSubject(response.IdToken);
        return new TokenAcquisitionResult(
            AccessToken: response.AccessToken,
            RefreshToken: response.RefreshToken ?? throw new InvalidOperationException("Refresh token missing — ensure offline_access scope is granted."),
            AccessTokenExpiresAt: DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
            Subject: subject ?? "unknown",
            Scope: response.Scope ?? string.Empty);
    }

    public async Task<AccessTokenLease> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var response = await PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = string.Join(' ', _options.Scopes)
        }, cancellationToken);

        return new AccessTokenLease(response.AccessToken, DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn));
    }

    private async Task<TokenResponse> PostTokenAsync(IDictionary<string, string> form, CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient("microsoft-graph-token");
        var url = string.Format(TokenUrlTemplate, _options.TenantId);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form!)
        };

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Microsoft Graph token endpoint returned {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Token exchange failed: {response.StatusCode}. Response: {Truncate(body, 240)}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Unable to deserialize token response.");
        return payload;
    }

    private static string? ExtractIdTokenSubject(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            return null;
        }

        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("preferred_username", out var upn))
            {
                return upn.GetString();
            }
            if (doc.RootElement.TryGetProperty("sub", out var sub))
            {
                return sub.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("id_token")] string? IdToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("token_type")] string? TokenType
    );
}
