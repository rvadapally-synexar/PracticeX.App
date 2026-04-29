namespace PracticeX.Discovery.Schemas;

/// <summary>
/// Call coverage agreements bind one or more covering physicians to a medical
/// group's call rotation - typically nights, weekends, holidays. They are the
/// contractual layer that scheduling engines (Qgenda, Amion, Tigerconnect, the
/// PCC scheduler) ultimately depend on. Extracting their structure cleanly is
/// the bridge between contract intelligence and scheduling.
/// </summary>
public static class CallCoverageSchemaV1Constants
{
    public const string Version = "call_coverage_v1";
    public const string CandidateType = "call_coverage_agreement";
}

public sealed record CallCoverageParty(
    string Role,                       // "medical_group" | "covering_physician" | "covered_facility"
    string Name,
    string? Specialty);

public sealed record CallCoverageWindow(
    string? CoverageType,              // "weekday_evenings" | "weekend" | "holiday" | "24x7"
    string? RawDescription);           // free-text description as captured

public sealed record CallCoverageCompensation(
    decimal? PerShiftAmount,
    decimal? PerDayAmount,
    decimal? PerMonthAmount,
    string? Currency,
    string? Notes);
