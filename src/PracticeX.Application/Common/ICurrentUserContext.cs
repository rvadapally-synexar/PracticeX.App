namespace PracticeX.Application.Common;

/// <summary>
/// Provides the active tenant + user for the current request. In production this is
/// resolved from the OIDC principal; for the demo path a default tenant/user are seeded.
/// </summary>
public interface ICurrentUserContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string ActorType { get; }
}
