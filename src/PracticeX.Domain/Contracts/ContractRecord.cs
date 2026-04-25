using PracticeX.Domain.Common;

namespace PracticeX.Domain.Contracts;

public sealed class ContractRecord : Entity
{
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid CounterpartyId { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public Guid? OwnerUserId { get; set; }
}

