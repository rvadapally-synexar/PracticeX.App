using PracticeX.Domain.Common;

namespace PracticeX.Domain.Sources;

public sealed class SourceConnection : Entity
{
    public Guid TenantId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string? DisplayName { get; set; }
    public string? OauthSubject { get; set; }
    public string? ScopeSet { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
}
