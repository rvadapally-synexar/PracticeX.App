using Microsoft.EntityFrameworkCore;
using PracticeX.Application.Common;
using PracticeX.Domain.Audit;
using PracticeX.Domain.Contracts;
using PracticeX.Domain.Documents;
using PracticeX.Domain.Organization;
using PracticeX.Domain.Sources;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure.Tenancy;

/// <summary>
/// Slice 21 — RBAC Phase 1: query-side access filters.
///
/// Centralizes the "what can this user see?" decision so endpoints don't
/// each reimplement the rule (and forget the edge cases). All filters are
/// fail-closed: a facility-scoped user without a matching assignment sees
/// nothing.
///
/// Usage pattern:
///   <code>
///   var docs = await db.DocumentCandidates
///       .Where(c =&gt; c.TenantId == userContext.TenantId)
///       .ApplyFacilityScope(userContext)
///       .ToListAsync(cancellationToken);
///   </code>
/// </summary>
public static class AccessFilterExtensions
{
    /// <summary>
    /// Filters a DocumentCandidates query by the caller's facility access
    /// set. Super-admin and org-admin pass through unchanged (the tenant
    /// filter the caller already applied is the boundary). Facility users
    /// see only candidates whose facility_hint_id is in their access set;
    /// candidates with no facility hint are hidden from facility users.
    /// </summary>
    public static IQueryable<DocumentCandidate> ApplyFacilityScope(
        this IQueryable<DocumentCandidate> query, ICurrentUserContext userContext)
    {
        if (userContext.IsSuperAdmin || userContext.IsOrgAdmin) return query;
        var allowed = userContext.AccessibleFacilityIds;
        if (allowed is null || allowed.Count == 0)
        {
            // No assignments → no access. Return empty (predicate is a
            // tautology that EF can fold into a constant).
            return query.Where(_ => false);
        }
        return query.Where(c => c.FacilityHintId.HasValue && allowed.Contains(c.FacilityHintId.Value));
    }

    /// <summary>
    /// Filters a DocumentAssets query by joining to the corresponding
    /// candidate row and gating on facility access. Use when the entity
    /// being queried is the asset (which doesn't directly carry facility
    /// hint today). Tenant filter must be applied by the caller.
    /// </summary>
    public static IQueryable<DocumentAsset> ApplyFacilityScopeViaCandidate(
        this IQueryable<DocumentAsset> assets, PracticeXDbContext db, ICurrentUserContext userContext)
    {
        if (userContext.IsSuperAdmin || userContext.IsOrgAdmin) return assets;
        var allowed = userContext.AccessibleFacilityIds;
        if (allowed is null || allowed.Count == 0)
        {
            return assets.Where(_ => false);
        }
        // Inner join: an asset is visible only if a candidate row exists
        // for it whose facility_hint_id is allowed. Assets without any
        // candidate (rare — pre-classification state) are hidden from
        // facility users by design.
        return assets.Where(a => db.DocumentCandidates
            .Any(c => c.DocumentAssetId == a.Id
                   && c.FacilityHintId.HasValue
                   && allowed.Contains(c.FacilityHintId.Value)));
    }

    /// <summary>
    /// Convenience: returns true if the current user can see the named
    /// facility (e.g., for surfacing it in the facility selector UI).
    /// </summary>
    public static bool CanSeeFacility(this ICurrentUserContext userContext, Guid facilityId) =>
        userContext.IsSuperAdmin
        || userContext.IsOrgAdmin
        || (userContext.AccessibleFacilityIds?.Contains(facilityId) ?? false);

    // ---------------------------------------------------------------------
    // Slice 21 Phase 2 — tenant-scope helpers.
    //
    // When a super-admin has not picked a tenant via X-Tenant-Override,
    // <see cref="ICurrentUserContext.IsCrossTenantView"/> is true and read
    // queries should NOT filter by tenant_id — they're meant to span every
    // tenant. Otherwise the predicate is the usual tenant equality.
    //
    // One overload per tenant-scoped entity. Verbose but safe — EF Core
    // has no trouble translating these (the lambda body is column equality
    // against a captured Guid). A generic interface would be tighter but
    // would require touching all 18 domain entities; the verbosity here is
    // contained to one file.
    // ---------------------------------------------------------------------

    public static IQueryable<DocumentAsset> ApplyTenantScope(
        this IQueryable<DocumentAsset> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<DocumentCandidate> ApplyTenantScope(
        this IQueryable<DocumentCandidate> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<SourceObject> ApplyTenantScope(
        this IQueryable<SourceObject> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<SourceConnection> ApplyTenantScope(
        this IQueryable<SourceConnection> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<IngestionBatch> ApplyTenantScope(
        this IQueryable<IngestionBatch> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<IngestionJob> ApplyTenantScope(
        this IQueryable<IngestionJob> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<ContractRecord> ApplyTenantScope(
        this IQueryable<ContractRecord> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<AuditEvent> ApplyTenantScope(
        this IQueryable<AuditEvent> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<Facility> ApplyTenantScope(
        this IQueryable<Facility> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<PortfolioBrief> ApplyTenantScope(
        this IQueryable<PortfolioBrief> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);

    public static IQueryable<CounselBrief> ApplyTenantScope(
        this IQueryable<CounselBrief> q, ICurrentUserContext u) =>
        u.IsCrossTenantView ? q : q.Where(x => x.TenantId == u.TenantId);
}
