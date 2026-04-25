using PracticeX.Domain.Common;

namespace PracticeX.Domain.Organization;

public sealed class Facility : Entity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Npi { get; set; }
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    public string? Specialty { get; set; }
    public string Status { get; set; } = "active";
}

