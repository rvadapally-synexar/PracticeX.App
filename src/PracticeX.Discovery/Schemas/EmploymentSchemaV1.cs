namespace PracticeX.Discovery.Schemas;

/// <summary>
/// Wire-shape constants and records for the Employment v1 contract family
/// (offer letter, engagement letter, advisor agreement, CIIA, PHI).
/// Mirrors docs/contract-schemas/employment_v1.md. Plain records — no EF
/// mapping or DI — so they round-trip cleanly through serialisation and
/// run identically in cloud orchestrator and desktop agent.
/// </summary>
public static class EmploymentSchemaV1Constants
{
    public const string Version = "employment_v1";

    public static class Subtypes
    {
        public const string OfferLetter = "offer_letter";
        public const string EngagementLetter = "engagement_letter";
        public const string AdvisorAgreement = "advisor_agreement";
        public const string Ciia = "ciia";
        public const string PhiAgreement = "phi_agreement";

        public static readonly IReadOnlyList<string> All =
            [OfferLetter, EngagementLetter, AdvisorAgreement, Ciia, PhiAgreement];
    }

    /// <summary>Classifier candidate type that maps to this family.</summary>
    public const string CandidateType = "employee_agreement";

    /// <summary>HIPAA default breach notification window (days) when not explicitly stated.</summary>
    public const int DefaultPhiBreachNotificationDays = 60;
}

public sealed record PartyRecord(
    string Type,
    string Name,
    string? Role,
    string? Title,
    AddressRecord? Address,
    string? Email
);

public sealed record AddressRecord(
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

public sealed record TermRecord(
    string Type,
    int? Months,
    DateTimeOffset? EndDate);

public sealed record MoneyRecord(decimal Amount, string Currency, string? Period);

public sealed record EquityGrant(
    string Type,
    decimal? PercentageOfFullyDiluted,
    long? ShareCount,
    VestingTerms Vesting,
    decimal? CapPercentage,
    ProRataIncrement? ProRata
);

public sealed record VestingTerms(
    int DurationMonths,
    int CliffMonths,
    string ScheduleAfterCliff,
    string? MilestoneTrigger);

public sealed record ProRataIncrement(decimal AmountPerUnit, string UnitDescription);

public sealed record SignatureRecord(
    string SignerName,
    string? SignerTitle,
    string? SignerRole,
    DateTimeOffset? SignedAtUtc,
    string SignatureProvider,
    string? EnvelopeId,
    int? PageNumber
);
