using Microsoft.EntityFrameworkCore;
using PracticeX.Application.Common;
using PracticeX.Domain.Organization;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure.Tenancy;

/// <summary>
/// Demo seeder for the default tenant + super-admin user. As of Slice 21
/// this no longer doubles as the <see cref="ICurrentUserContext"/>
/// implementation — that's now <c>RequestScopedCurrentUserContext</c>,
/// which resolves the user from the Cloudflare Access principal. This
/// class only exists to seed the row that the resolver looks up.
/// </summary>
public static class DemoCurrentUserContext
{
    // Slice 21 Phase 2 (renumber): the original seed used pattern UUIDs
    // (`11111111-...`, `22222222-...`) that read as obviously fake in
    // URLs and audit logs. The renumber migration moved them to real v4
    // UUIDs; the constants here track that move so a fresh-db dev start
    // produces the same ids as a renumbered prod db. See
    // migrations/20260510_renumber_placeholder_ids.sql for the full map.
    private static readonly Guid DemoTenantId = new("02b32f45-2ad4-4aa3-865a-6150d8fd3f98");
    private static readonly Guid DemoUserId = new("ed785f04-c5a8-4539-ae4c-2f41ed002477");

    public static async Task EnsureSeededAsync(PracticeXDbContext dbContext, CancellationToken cancellationToken)
    {
        // Lookup by id (post-renumber); if not found, fall back to lookup
        // by name. The fallback covers the transition window where the
        // renumber migration hasn't run yet — without it, the seeder
        // would insert a SECOND platform tenant row with the new id while
        // the old one still exists, leaving the DB in a duplicate state.
        // Once the migration runs the by-id lookup wins.
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == DemoTenantId, cancellationToken);
        tenant ??= await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Name == "PracticeX Platform" || t.Name == "PracticeX",
                cancellationToken);
        if (tenant is null)
        {
            dbContext.Tenants.Add(new Tenant
            {
                Id = DemoTenantId,
                Name = "PracticeX Platform",
                Status = "active",
                DataRegion = "us",
                BaaStatus = "signed",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else if (tenant.Name == "PracticeX Demo Group" || tenant.Name == "PracticeX")
        {
            // Backfill old demo seed name on existing rows. Slice 21
            // Phase 2 renamed the umbrella to "PracticeX Platform".
            tenant.Name = "PracticeX Platform";
            tenant.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // User lookup: prefer id (post-renumber), then fall back to email
        // (so a pre-renumber DB with the demo user under the old id keeps
        // working until the migration moves the row).
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == DemoUserId, cancellationToken);
        user ??= await dbContext.Users.FirstOrDefaultAsync(
            u => u.Email == "rvadapally@practicex.ai" || u.Email == "rvadapally@synexar.ai",
            cancellationToken);
        if (user is null)
        {
            // Use the resolved tenant's id so this works against either a
            // pre-renumber (old id) or post-renumber (new id) DB.
            dbContext.Users.Add(new AppUser
            {
                Id = DemoUserId,
                TenantId = tenant?.Id ?? DemoTenantId,
                Email = "rvadapally@practicex.ai",
                Name = "Raghuram Vadapally",
                Status = "active",
                IsSuperAdmin = true,  // Slice 21: seed as super-admin
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else if (user.Name == "Jordan Okafor" || user.Email == "demo@practicex.com")
        {
            user.Name = "Raghuram Vadapally";
            user.Email = "rvadapally@practicex.ai";
            user.IsSuperAdmin = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (!user.IsSuperAdmin)
        {
            // Backfill: the seeded demo user must always be super-admin so
            // the no-header local dev path resolves to a usable principal.
            user.IsSuperAdmin = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
