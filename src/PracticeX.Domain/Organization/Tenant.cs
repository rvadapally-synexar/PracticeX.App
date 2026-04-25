using PracticeX.Domain.Common;

namespace PracticeX.Domain.Organization;

public sealed class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string DataRegion { get; set; } = "us";
    public string BaaStatus { get; set; } = "pending";
}

