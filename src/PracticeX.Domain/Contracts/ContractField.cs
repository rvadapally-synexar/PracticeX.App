using System.Text.Json;
using PracticeX.Domain.Common;

namespace PracticeX.Domain.Contracts;

public sealed class ContractField : Entity
{
    public Guid TenantId { get; set; }
    public Guid ContractId { get; set; }
    public string SchemaVersion { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public JsonDocument ValueJson { get; set; } = JsonDocument.Parse("{}");
    public string? NormalizedValue { get; set; }
    public decimal Confidence { get; set; }
    public string ReviewStatus { get; set; } = "candidate";
}

