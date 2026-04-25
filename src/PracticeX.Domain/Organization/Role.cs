using System.Text.Json;
using PracticeX.Domain.Common;

namespace PracticeX.Domain.Organization;

public sealed class Role : Entity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public JsonDocument Permissions { get; set; } = JsonDocument.Parse("{}");
}

