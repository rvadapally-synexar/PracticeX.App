using System.Text.Json;
using PracticeX.Domain.Common;

namespace PracticeX.Domain.Contracts;

public sealed class Counterparty : Entity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public JsonDocument Aliases { get; set; } = JsonDocument.Parse("[]");
    public string? PayerIdentifier { get; set; }
}

