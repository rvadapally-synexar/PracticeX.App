using PracticeX.Domain.Common;

namespace PracticeX.Domain.Organization;

public sealed class AppUser : Entity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "invited";
    public DateTimeOffset? LastLoginAt { get; set; }
}

