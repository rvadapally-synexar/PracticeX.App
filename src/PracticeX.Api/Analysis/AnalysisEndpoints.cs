using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeX.Application.Common;
using PracticeX.Domain.Documents;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Api.Analysis;

/// <summary>
/// "Premium analysis surface" endpoints — the read side that consumes the
/// extraction pipeline output (classification + layout + field extraction)
/// and presents it as a categorized portfolio view. This is what the demo
/// surface for the board meeting renders against.
/// </summary>
public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/analysis").WithTags("Analysis");

        group.MapGet("/portfolio", GetPortfolio).WithName("GetPortfolio");
        group.MapGet("/documents/{assetId:guid}", GetDocumentDetail).WithName("GetDocumentDetail");
        group.MapGet("/insights", GetCrossDocumentInsights).WithName("GetCrossDocumentInsights");

        return routes;
    }

    private static async Task<Ok<PortfolioResponse>> GetPortfolio(
        PracticeXDbContext db,
        ICurrentUserContext userContext,
        CancellationToken cancellationToken)
    {
        var assets = await db.DocumentAssets
            .Where(a => a.TenantId == userContext.TenantId)
            .Join(db.DocumentCandidates,
                a => a.Id,
                c => c.DocumentAssetId,
                (a, c) => new { Asset = a, Candidate = c })
            .ToListAsync(cancellationToken);

        var sourceObjectIds = assets
            .Where(x => x.Asset.SourceObjectId.HasValue)
            .Select(x => x.Asset.SourceObjectId!.Value)
            .Distinct()
            .ToList();

        var sourceObjects = await db.SourceObjects
            .Where(s => sourceObjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var docs = assets.Select(x => new PortfolioDocument(
            DocumentAssetId: x.Asset.Id,
            DocumentCandidateId: x.Candidate.Id,
            FileName: x.Asset.SourceObjectId.HasValue && sourceObjects.TryGetValue(x.Asset.SourceObjectId.Value, out var name) ? name : "(unnamed)",
            CandidateType: x.Candidate.CandidateType,
            ExtractedSubtype: x.Asset.ExtractedSubtype,
            Confidence: x.Candidate.Confidence,
            PageCount: x.Asset.PageCount,
            SizeBytes: x.Asset.SizeBytes,
            HasTextLayer: x.Asset.HasTextLayer,
            UsedDocIntelligence: x.Asset.LayoutProvider != null,
            LayoutPageCount: x.Asset.LayoutPageCount,
            ExtractionStatus: x.Asset.ExtractionStatus,
            ExtractionSchemaVersion: x.Asset.ExtractedSchemaVersion,
            IsTemplate: x.Asset.ExtractedIsTemplate,
            IsExecuted: x.Asset.ExtractedIsExecuted,
            CreatedAt: x.Asset.CreatedAt
        )).OrderByDescending(d => d.SizeBytes).ToList();

        // Family rollups - group by candidate type with totals.
        var families = docs
            .GroupBy(d => MapToFamily(d.CandidateType))
            .Select(g => new FamilyRollup(
                Family: g.Key,
                DocumentCount: g.Count(),
                TotalPages: g.Sum(d => d.PageCount ?? 0),
                TotalSizeMb: Math.Round(g.Sum(d => d.SizeBytes) / 1024m / 1024m, 2),
                DocIntelPagesUsed: g.Where(d => d.UsedDocIntelligence).Sum(d => d.LayoutPageCount ?? 0),
                Documents: g.Select(d => d.FileName).ToList()))
            .OrderByDescending(f => f.DocumentCount)
            .ToList();

        // Cost estimate: $0.001 per Doc Intel page (prebuilt-layout S0).
        var totalDocIntelPages = docs.Sum(d => d.LayoutPageCount ?? 0);
        var estimatedDocIntelCost = Math.Round(totalDocIntelPages * 0.001m, 4);

        return TypedResults.Ok(new PortfolioResponse(
            TenantId: userContext.TenantId,
            TotalDocuments: docs.Count,
            TotalPages: docs.Sum(d => d.PageCount ?? 0),
            TotalSizeMb: Math.Round(docs.Sum(d => d.SizeBytes) / 1024m / 1024m, 2),
            DocIntelPagesProcessed: totalDocIntelPages,
            EstimatedDocIntelCostUsd: estimatedDocIntelCost,
            Families: families,
            Documents: docs
        ));
    }

    private static async Task<Results<Ok<DocumentDetailResponse>, NotFound>> GetDocumentDetail(
        Guid assetId,
        PracticeXDbContext db,
        ICurrentUserContext userContext,
        CancellationToken cancellationToken)
    {
        var asset = await db.DocumentAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.TenantId == userContext.TenantId, cancellationToken);
        if (asset is null) return TypedResults.NotFound();

        var candidate = await db.DocumentCandidates
            .FirstOrDefaultAsync(c => c.DocumentAssetId == assetId && c.TenantId == userContext.TenantId, cancellationToken);

        string? fileName = null;
        if (asset.SourceObjectId.HasValue)
        {
            fileName = await db.SourceObjects
                .Where(s => s.Id == asset.SourceObjectId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Parse extracted_fields_json into structured response.
        ExtractedFieldsView? extractedFields = null;
        if (!string.IsNullOrEmpty(asset.ExtractedFieldsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(asset.ExtractedFieldsJson);
                var root = doc.RootElement;
                var fields = new List<ExtractedFieldView>();
                if (root.TryGetProperty("fields", out var fieldsEl) && fieldsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in fieldsEl.EnumerateArray())
                    {
                        var name = f.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var confidence = f.TryGetProperty("confidence", out var c) && c.TryGetDecimal(out var cd) ? cd : 0m;
                        var sourceCitation = f.TryGetProperty("sourceCitation", out var sc) ? sc.GetString() : null;
                        var value = f.TryGetProperty("value", out var v) ? v.ToString() : null;
                        fields.Add(new ExtractedFieldView(name, value, confidence, sourceCitation));
                    }
                }
                var reasonCodes = root.TryGetProperty("reasonCodes", out var rc) && rc.ValueKind == JsonValueKind.Array
                    ? rc.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : new List<string>();
                extractedFields = new ExtractedFieldsView(fields, reasonCodes);
            }
            catch { /* leave null on parse failure */ }
        }

        // Pull a layout text snippet (first ~600 chars) for the demo's "see what we extracted" panel.
        string? layoutSnippet = null;
        if (!string.IsNullOrEmpty(asset.LayoutJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(asset.LayoutJson);
                if (doc.RootElement.TryGetProperty("fullText", out var ft))
                {
                    var text = ft.GetString() ?? "";
                    layoutSnippet = text.Length > 600 ? text[..600] + "..." : text;
                }
            }
            catch { /* ignore */ }
        }

        return TypedResults.Ok(new DocumentDetailResponse(
            DocumentAssetId: asset.Id,
            FileName: fileName ?? "(unnamed)",
            CandidateType: candidate?.CandidateType,
            Confidence: candidate?.Confidence,
            ExtractedSubtype: asset.ExtractedSubtype,
            ExtractedSchemaVersion: asset.ExtractedSchemaVersion,
            ExtractorName: asset.ExtractorName,
            ExtractionStatus: asset.ExtractionStatus,
            IsTemplate: asset.ExtractedIsTemplate,
            IsExecuted: asset.ExtractedIsExecuted,
            PageCount: asset.PageCount,
            HasTextLayer: asset.HasTextLayer,
            LayoutProvider: asset.LayoutProvider,
            LayoutModel: asset.LayoutModel,
            LayoutPageCount: asset.LayoutPageCount,
            LayoutSnippet: layoutSnippet,
            ExtractedFields: extractedFields,
            CreatedAt: asset.CreatedAt
        ));
    }

    private static async Task<Ok<CrossDocumentInsights>> GetCrossDocumentInsights(
        PracticeXDbContext db,
        ICurrentUserContext userContext,
        CancellationToken cancellationToken)
    {
        var assets = await db.DocumentAssets
            .Where(a => a.TenantId == userContext.TenantId && a.ExtractedFieldsJson != null)
            .ToListAsync(cancellationToken);

        // Build a registry of premises addresses across all leases.
        var addressByDoc = new Dictionary<string, string>();
        decimal totalSqft = 0m;
        var amendmentChains = new Dictionary<string, List<string>>();
        var landlords = new HashSet<string>();
        var tenants = new HashSet<string>();
        var counterparties = new HashSet<string>();

        foreach (var asset in assets)
        {
            var docName = await db.SourceObjects
                .Where(s => s.Id == asset.SourceObjectId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "(unnamed)";

            try
            {
                using var doc = JsonDocument.Parse(asset.ExtractedFieldsJson!);
                var root = doc.RootElement;
                if (!root.TryGetProperty("fields", out var fields)) continue;

                foreach (var f in fields.EnumerateArray())
                {
                    var name = f.TryGetProperty("name", out var nEl) ? nEl.GetString() ?? "" : "";
                    if (!f.TryGetProperty("value", out var v) || v.ValueKind == JsonValueKind.Null) continue;

                    if (name == "premises" && v.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in v.EnumerateArray())
                        {
                            var sqft = p.TryGetProperty("RentableSquareFeet", out var s) && s.TryGetDecimal(out var sd) ? sd : 0m;
                            var street = p.TryGetProperty("StreetAddress", out var st) ? st.GetString() : null;
                            totalSqft += sqft;
                            if (!string.IsNullOrWhiteSpace(street))
                            {
                                addressByDoc[docName] = street!;
                            }
                        }
                    }
                    else if (name == "landlord" && v.ValueKind == JsonValueKind.String)
                    {
                        var s = v.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) landlords.Add(s!);
                    }
                    else if (name == "tenant" && v.ValueKind == JsonValueKind.String)
                    {
                        var s = v.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) tenants.Add(s!);
                    }
                    else if (name == "amends" && v.ValueKind == JsonValueKind.Object)
                    {
                        var parentTitle = v.TryGetProperty("ParentDocumentTitle", out var pt) ? pt.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(parentTitle))
                        {
                            if (!amendmentChains.TryGetValue(parentTitle!, out var chain))
                            {
                                chain = new List<string>();
                                amendmentChains[parentTitle!] = chain;
                            }
                            chain.Add(docName);
                        }
                    }
                    else if (name == "parties" && v.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var party in v.EnumerateArray())
                        {
                            var partyName = party.TryGetProperty("Name", out var pn) ? pn.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(partyName)) counterparties.Add(partyName!);
                        }
                    }
                }
            }
            catch { /* skip malformed */ }
        }

        return TypedResults.Ok(new CrossDocumentInsights(
            TotalRentableSqft: totalSqft > 0 ? totalSqft : null,
            UniqueLandlords: landlords.OrderBy(s => s).ToList(),
            UniqueTenants: tenants.OrderBy(s => s).ToList(),
            UniqueCounterparties: counterparties.OrderBy(s => s).ToList(),
            AmendmentChains: amendmentChains
                .Select(kvp => new AmendmentChain(kvp.Key, kvp.Value))
                .OrderByDescending(c => c.Amendments.Count)
                .ToList(),
            DocumentAddresses: addressByDoc
        ));
    }

    private static string MapToFamily(string candidateType) => candidateType switch
    {
        DocumentCandidateTypes.Lease or
        DocumentCandidateTypes.LeaseAmendment or
        DocumentCandidateTypes.LeaseLoi => "lease",

        DocumentCandidateTypes.EmployeeAgreement or
        DocumentCandidateTypes.Amendment => "employment_governance",

        DocumentCandidateTypes.Nda => "nda",

        DocumentCandidateTypes.Bylaws => "governance",

        DocumentCandidateTypes.CallCoverageAgreement => "scheduling",

        DocumentCandidateTypes.ServiceAgreement or
        DocumentCandidateTypes.VendorContract => "vendor_services",

        DocumentCandidateTypes.PayerContract => "payer",
        DocumentCandidateTypes.ProcessorAgreement => "compliance",
        DocumentCandidateTypes.FeeSchedule => "fee_schedule",
        _ => "unclassified"
    };
}

// ----------------------------------------------------------------------------
// Response DTOs
// ----------------------------------------------------------------------------

public sealed record PortfolioResponse(
    Guid TenantId,
    int TotalDocuments,
    int TotalPages,
    decimal TotalSizeMb,
    int DocIntelPagesProcessed,
    decimal EstimatedDocIntelCostUsd,
    IReadOnlyList<FamilyRollup> Families,
    IReadOnlyList<PortfolioDocument> Documents);

public sealed record FamilyRollup(
    string Family,
    int DocumentCount,
    int TotalPages,
    decimal TotalSizeMb,
    int DocIntelPagesUsed,
    IReadOnlyList<string> Documents);

public sealed record PortfolioDocument(
    Guid DocumentAssetId,
    Guid DocumentCandidateId,
    string FileName,
    string CandidateType,
    string? ExtractedSubtype,
    decimal Confidence,
    int? PageCount,
    long SizeBytes,
    bool? HasTextLayer,
    bool UsedDocIntelligence,
    int? LayoutPageCount,
    string? ExtractionStatus,
    string? ExtractionSchemaVersion,
    bool? IsTemplate,
    bool? IsExecuted,
    DateTimeOffset CreatedAt);

public sealed record DocumentDetailResponse(
    Guid DocumentAssetId,
    string FileName,
    string? CandidateType,
    decimal? Confidence,
    string? ExtractedSubtype,
    string? ExtractedSchemaVersion,
    string? ExtractorName,
    string? ExtractionStatus,
    bool? IsTemplate,
    bool? IsExecuted,
    int? PageCount,
    bool? HasTextLayer,
    string? LayoutProvider,
    string? LayoutModel,
    int? LayoutPageCount,
    string? LayoutSnippet,
    ExtractedFieldsView? ExtractedFields,
    DateTimeOffset CreatedAt);

public sealed record ExtractedFieldsView(
    IReadOnlyList<ExtractedFieldView> Fields,
    IReadOnlyList<string> ReasonCodes);

public sealed record ExtractedFieldView(
    string Name,
    string? Value,
    decimal Confidence,
    string? SourceCitation);

public sealed record CrossDocumentInsights(
    decimal? TotalRentableSqft,
    IReadOnlyList<string> UniqueLandlords,
    IReadOnlyList<string> UniqueTenants,
    IReadOnlyList<string> UniqueCounterparties,
    IReadOnlyList<AmendmentChain> AmendmentChains,
    IReadOnlyDictionary<string, string> DocumentAddresses);

public sealed record AmendmentChain(
    string ParentDocumentTitle,
    IReadOnlyList<string> Amendments);
