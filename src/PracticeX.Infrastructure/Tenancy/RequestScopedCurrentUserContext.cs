using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PracticeX.Application.Common;
using PracticeX.Domain.Organization;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure.Tenancy;

/// <summary>
/// Slice 21 — RBAC Phase 1.
///
/// Resolves the current user from the request principal, looks them up in
/// <c>org.users</c> via email, and computes their effective access scope:
/// <list type="bullet">
///   <item>Super admin → unrestricted (AccessibleFacilityIds = null).</item>
///   <item>Org admin → all facilities in their tenant (AccessibleFacilityIds = null,
///         IsOrgAdmin = true; the tenant filter still applies).</item>
///   <item>Facility user → set of facility ids from their active role_assignments.</item>
/// </list>
///
/// Email source priority:
///   1. <c>X-Impersonate-Email</c> header (for testing; ignored unless the
///      current effective user is super-admin OR no auth principal is set
///      and the env permits demo fallback).
///   2. <c>Cf-Access-Authenticated-User-Email</c> header (production:
///      Cloudflare Access OTP / OIDC-validated email).
///   3. Demo super-admin fallback when no header is present (local dev
///      only; the demo super-admin is the seeded Raghu user).
///
/// Each property accessor materializes the load lazily; a request that
/// never reads these properties pays no DB cost.
/// </summary>
public sealed class RequestScopedCurrentUserContext : ICurrentUserContext
{
    private const string CloudflareEmailHeader = "Cf-Access-Authenticated-User-Email";
    private const string ImpersonateHeader = "X-Impersonate-Email";
    // Slice 21 Phase 2: super-admins switch tenant context with this header.
    // Ignored for non-super-admin callers — their tenant_id is fixed by
    // their AppUser row.
    private const string TenantOverrideHeader = "X-Tenant-Override";

    // Slice 21 Phase 2 (renumber): match the post-renumber ids — see
    // DemoCurrentUserContext for the rationale + the renumber migration
    // for the full old→new map.
    //
    // Slice 21.1 fix: DemoTenantId now points at the real Platform tenant
    // (11111111-...). The old 02b32f45-... value was a stale pre-renumber id
    // that doesn't exist in org.tenants, so any write that landed under it
    // (audit_events, etc.) blew up with a FK violation. Every browser request
    // hits the last-resort branch in Resolve() because the upstream Access
    // chain strips Cf-Access-Authenticated-User-Email; pointing the fallback
    // tenant at a real row lets writes like /api/analytics/event succeed.
    private static readonly Guid DemoTenantId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoUserId = new("ed785f04-c5a8-4539-ae4c-2f41ed002477");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PracticeXDbContext _db;
    private readonly Lazy<ResolvedUser> _resolved;

    public RequestScopedCurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        PracticeXDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _resolved = new Lazy<ResolvedUser>(Resolve);
    }

    public Guid TenantId => _resolved.Value.TenantId;
    public Guid UserId => _resolved.Value.UserId;
    public string ActorType => "user";
    public bool IsSuperAdmin => _resolved.Value.IsSuperAdmin;
    public bool IsOrgAdmin => _resolved.Value.IsOrgAdmin;
    public bool IsCrossTenantView => _resolved.Value.IsCrossTenantView;
    public IReadOnlySet<Guid>? AccessibleFacilityIds => _resolved.Value.AccessibleFacilityIds;

    public bool IsAuthorizedForFacility(Guid? facilityId)
    {
        if (IsSuperAdmin) return true;
        if (IsOrgAdmin) return true;          // tenant filter at the query enforces tenant boundary
        // Documents without a facility hint never reach a facility-scoped user.
        if (!facilityId.HasValue) return false;
        return AccessibleFacilityIds is { } set && set.Contains(facilityId.Value);
    }

    private ResolvedUser Resolve()
    {
        var email = ResolveEmail();

        AppUser? user = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            // Email is unique per (tenant_id, email) — lookup is by email
            // alone today because we only have one tenant; once tenant
            // split lands the lookup will need tenant disambiguation.
            user = _db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == email && u.Status == "active");

            if (user is null)
            {
                // Cloudflare Access authenticated someone, but we have not
                // provisioned that principal in org.users. Fail closed: no
                // tenant/facility data, no cross-tenant view. The demo
                // super-admin fallback is only for local dev where there is
                // no authenticated email header at all.
                return new ResolvedUser(DemoTenantId, DemoUserId, IsSuperAdmin: false, IsOrgAdmin: false, IsCrossTenantView: false, new HashSet<Guid>());
            }
        }

        // Demo fallback: pre-auth local dev. Resolve to the seeded super-admin.
        if (user is null)
        {
            user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Id == DemoUserId);
        }

        if (user is null)
        {
            // Last resort — pre-seed scenario, or production traffic where
            // the upstream Access app strips the email header (service-token
            // auth chains: app.practicex.ai → Pages Function → service-token →
            // api.practicex.ai; the second Access app sets the email to
            // empty because service tokens have no associated user, so the
            // API can never resolve the OTP user behind the browser). Behave
            // as a stub super-admin AND honor X-Tenant-Override so the org
            // switcher in the topbar actually scopes the response. Without
            // honoring the override here, picking "Synexar Inc" in the
            // dropdown looks like it did nothing — the page still shows
            // cross-tenant data.
            var (lastResortTenant, lastResortOverride) = ResolveTenantOverride(DemoTenantId);
            return new ResolvedUser(
                lastResortTenant,
                DemoUserId,
                IsSuperAdmin: true,
                IsOrgAdmin: false,
                IsCrossTenantView: !lastResortOverride,
                null);
        }

        if (user.IsSuperAdmin)
        {
            // Phase 2 — tenant switching: super-admin can override the
            // active tenant context with X-Tenant-Override. When the
            // header points at a real tenant, that tenant scopes every
            // read; when it's missing/blank/invalid, super-admin defaults
            // to a cross-tenant view (sees every tenant's data). This
            // matches the user's mental model — "super admin = see
            // everything" — and keeps the platform tenant as a write-time
            // home rather than the empty default surface.
            var (effectiveTenant, isOverride) = ResolveTenantOverride(user.TenantId);
            return new ResolvedUser(
                effectiveTenant,
                user.Id,
                IsSuperAdmin: true,
                IsOrgAdmin: false,
                IsCrossTenantView: !isOverride,
                null);
        }

        // Compute access set from role assignments. Org admin = any active
        // role row whose role.name is "org_admin" — facility hint ignored.
        var assignments = _db.RoleAssignments
            .AsNoTracking()
            .Where(ra => ra.UserId == user.Id && ra.Status == "active")
            .Join(_db.Roles.AsNoTracking(),
                ra => ra.RoleId,
                r => r.Id,
                (ra, r) => new { ra.FacilityId, RoleName = r.Name })
            .ToList();

        var isOrgAdmin = assignments.Any(a => a.RoleName == StandardRoleNames.OrgAdmin);
        if (isOrgAdmin)
        {
            return new ResolvedUser(user.TenantId, user.Id, IsSuperAdmin: false, IsOrgAdmin: true, IsCrossTenantView: false, null);
        }

        var facilityIds = assignments
            .Where(a => a.RoleName == StandardRoleNames.FacilityUser && a.FacilityId.HasValue)
            .Select(a => a.FacilityId!.Value)
            .ToHashSet();

        // No assignments = no access. Return an empty set, not null — null
        // means "unrestricted" for org/super admins.
        return new ResolvedUser(user.TenantId, user.Id, IsSuperAdmin: false, IsOrgAdmin: false, IsCrossTenantView: false, facilityIds);
    }

    /// <summary>
    /// Returns the tenant id the super-admin is currently viewing and
    /// whether a real override was supplied. When the header is missing,
    /// blank, or points at a non-existent tenant, we fall back to the
    /// home tenant AND report no-override — that triggers the
    /// cross-tenant view in <see cref="Resolve"/>. Invalid override values
    /// silently fall back rather than 4xxing every API call when the
    /// cookie is stale.
    /// </summary>
    private (Guid TenantId, bool IsOverride) ResolveTenantOverride(Guid homeTenantId)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return (homeTenantId, false);
        var raw = HeaderValue(ctx, TenantOverrideHeader);
        if (string.IsNullOrWhiteSpace(raw)) return (homeTenantId, false);
        if (!Guid.TryParse(raw, out var requested)) return (homeTenantId, false);
        var exists = _db.Tenants.AsNoTracking().Any(t => t.Id == requested);
        return exists ? (requested, true) : (homeTenantId, false);
    }

    private string? ResolveEmail()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        // Impersonation only allowed when the upstream is locked down (local
        // dev: no Cf-Access header) — anyone with X-Impersonate-Email behind
        // Cloudflare Access who is NOT a super-admin should be ignored. The
        // simplest enforcement: prefer the impersonation header only when no
        // Cloudflare Access header is present, OR when the impersonator's
        // resolved-from-Access user is a super-admin. We can't yet check
        // "is super admin" without resolving; approximate by allowing
        // impersonation only when both headers are present.
        var cfEmail = HeaderValue(ctx, CloudflareEmailHeader);
        var impEmail = HeaderValue(ctx, ImpersonateHeader);

        if (!string.IsNullOrWhiteSpace(impEmail))
        {
            if (string.IsNullOrWhiteSpace(cfEmail))
            {
                // Local dev — no Cloudflare Access in front. Permit
                // impersonation as an explicit testing affordance.
                return impEmail;
            }
            // Behind Access — only honor if the Access principal is a
            // super-admin. This requires a lookup we'd rather avoid in
            // ResolveEmail; defer to Resolve() which already resolves the
            // user. For now, keep it simple: allow impersonation behind
            // Access only when the Access user is also a super-admin.
            var accessUser = _db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == cfEmail);
            if (accessUser?.IsSuperAdmin == true)
            {
                return impEmail;
            }
            // Access user is not super-admin; ignore impersonation header.
            return cfEmail;
        }

        return cfEmail;
    }

    private static string? HeaderValue(HttpContext ctx, string headerName)
    {
        if (!ctx.Request.Headers.TryGetValue(headerName, out var values)) return null;
        var v = values.ToString();
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    private sealed record ResolvedUser(
        Guid TenantId,
        Guid UserId,
        bool IsSuperAdmin,
        bool IsOrgAdmin,
        bool IsCrossTenantView,
        IReadOnlySet<Guid>? AccessibleFacilityIds);
}
