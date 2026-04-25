using PracticeX.Domain.Common;

namespace PracticeX.Domain.Sources;

public sealed class SourceConnection : Entity
{
    public Guid TenantId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Status { get; set; } = SourceConnectionStatus.Draft;
    public string? DisplayName { get; set; }
    public string? OauthSubject { get; set; }
    public string? ScopeSet { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }

    public string? ConfigJson { get; set; }
    public string? CredentialsJson { get; set; }
    public string? LastError { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public static class SourceConnectionStatus
{
    public const string Draft = "draft";
    public const string AwaitingAuth = "awaiting_auth";
    public const string Connected = "connected";
    public const string Error = "error";
    public const string Disabled = "disabled";
}

public static class SourceTypes
{
    public const string LocalFolder = "local_folder";
    public const string OutlookMailbox = "outlook_mailbox";
}
