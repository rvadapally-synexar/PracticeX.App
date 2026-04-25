using PracticeX.Domain.Common;

namespace PracticeX.Domain.Organization;

public sealed class RoleAssignment : Entity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid RoleId { get; set; }
    public string Status { get; set; } = "active";
}

