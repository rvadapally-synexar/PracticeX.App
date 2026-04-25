namespace PracticeX.Application.SourceDiscovery.Outlook;

public sealed class MicrosoftGraphOptions
{
    public const string SectionName = "MicrosoftGraph";

    /// <summary>App registration (client) id from Microsoft Entra.</summary>
    public string? ClientId { get; set; }

    /// <summary>Azure AD tenant id, or "common" for multi-tenant.</summary>
    public string TenantId { get; set; } = "common";

    /// <summary>App registration client secret. Read from secrets manager in production.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>OAuth redirect URI registered for the app.</summary>
    public string RedirectUri { get; set; } = "https://localhost:7100/api/sources/outlook/oauth/callback";

    /// <summary>Read-only Graph scopes. Refresh token requested via offline_access.</summary>
    public IList<string> Scopes { get; } = new List<string>
    {
        "offline_access",
        "Mail.Read",
        "Mail.ReadBasic"
    };
}
