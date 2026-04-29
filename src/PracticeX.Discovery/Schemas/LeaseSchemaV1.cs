namespace PracticeX.Discovery.Schemas;

/// <summary>
/// Lease family schema v1. Real-estate leases are a major cost line for any
/// healthcare practice and often have long amendment chains (Eagle GI: 8+
/// amendments stretching from 1999). The schema is shared across master leases,
/// lease amendments, and lease LOIs because they reference the same shape;
/// the subtype discriminator drives which fields are required.
/// </summary>
public static class LeaseSchemaV1Constants
{
    public const string Version = "lease_v1";

    /// <summary>
    /// Candidate type returned by the rule classifier when this extractor
    /// should be invoked.
    /// </summary>
    public const string CandidateType = "lease";

    public static class Subtypes
    {
        public const string MasterLease = "master_lease";
        public const string LeaseAmendment = "lease_amendment";
        public const string LeaseLoi = "lease_loi";
        public const string SubLease = "sublease";

        public static readonly string[] All = [MasterLease, LeaseAmendment, LeaseLoi, SubLease];
    }
}

/// <summary>
/// One leased space. A single lease can have multiple Premises records
/// (Eagle GI's leases typically reference 3+ suites in the same building).
/// </summary>
public sealed record LeasePremises(
    string? StreetAddress,
    string? City,
    string? State,
    string? PostalCode,
    string? BuildingName,
    string? Suite,
    decimal? RentableSquareFeet);

/// <summary>
/// Rent terms — base amount plus optional escalation pattern.
/// </summary>
public sealed record LeaseRent(
    MoneyRecord? BaseRent,           // e.g. ($25,000, USD, "month")
    string? EscalationPattern,        // e.g. "3% annual", "CPI", "deferred"
    DateTimeOffset? RentStartDate,
    bool DeferredFlag);

/// <summary>
/// Reference to a parent agreement when this document is an amendment / addendum.
/// </summary>
public sealed record LeaseAmendmentRef(
    DateTimeOffset? ParentEffectiveDate,
    string? ParentDocumentTitle,      // free-text, e.g. "Lease Agreement dated August 31, 2005"
    int? AmendmentNumber);            // e.g. 4 for "4th Amendment"
