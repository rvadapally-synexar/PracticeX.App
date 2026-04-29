using Microsoft.EntityFrameworkCore;
using PracticeX.Application.Common;
using PracticeX.Domain.Organization;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure.Tenancy;

/// <summary>
/// Demo / pilot current-user resolver. Reads or seeds a default tenant + user so
/// the source discovery flow works without authentication wired up. Production
/// replaces this with a principal-backed implementation.
/// </summary>
public sealed class DemoCurrentUserContext : ICurrentUserContext
{
    private static readonly Guid DemoTenantId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoUserId = new("22222222-2222-2222-2222-222222222222");

    public Guid TenantId => DemoTenantId;
    public Guid UserId => DemoUserId;
    public string ActorType => "user";

    public static async Task EnsureSeededAsync(PracticeXDbContext dbContext, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == DemoTenantId, cancellationToken);
        if (tenant is null)
        {
            dbContext.Tenants.Add(new Tenant
            {
                Id = DemoTenantId,
                Name = "PracticeX",
                Status = "active",
                DataRegion = "us",
                BaaStatus = "signed",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else if (tenant.Name == "PracticeX Demo Group")
        {
            // Backfill old demo seed name on existing rows.
            tenant.Name = "PracticeX";
            tenant.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == DemoUserId, cancellationToken);
        if (user is null)
        {
            dbContext.Users.Add(new AppUser
            {
                Id = DemoUserId,
                TenantId = DemoTenantId,
                Email = "rvadapally@practicex.ai",
                Name = "Raghuram Vadapally",
                Status = "active",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else if (user.Name == "Jordan Okafor" || user.Email == "demo@practicex.com")
        {
            user.Name = "Raghuram Vadapally";
            user.Email = "rvadapally@practicex.ai";
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
