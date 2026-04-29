using System.Globalization;
using System.Text.RegularExpressions;
using PracticeX.Discovery.FieldExtraction.Helpers;
using PracticeX.Discovery.Schemas;

namespace PracticeX.Discovery.FieldExtraction;

/// <summary>
/// v1 call-coverage extractor. The contract↔scheduling bridge: parses the
/// rotation rules + compensation structure that scheduling tools take as
/// input. Pure regex over layout/local text; no LLM in v1.
/// </summary>
public sealed class CallCoverageExtractor : IContractFieldExtractor
{
    public string Name => "call-coverage-extractor-v1";
    public string SchemaVersion => CallCoverageSchemaV1Constants.Version;

    public bool CanExtract(string subtypeOrCandidateType) =>
        string.Equals(subtypeOrCandidateType, CallCoverageSchemaV1Constants.CandidateType, StringComparison.OrdinalIgnoreCase);

    public FieldExtractionResult Extract(FieldExtractionInput input)
    {
        var body = input.FullText ?? string.Empty;
        var fields = new Dictionary<string, ExtractedField>(StringComparer.OrdinalIgnoreCase);
        var reasons = new List<string> { "call_coverage_extractor_v1" };
        var isExecuted = !string.IsNullOrWhiteSpace(input.SignatureProvider);

        // ---- Effective date ----
        var (effectiveDate, dateCitation) = ExtractEffectiveDate(body);
        fields["effective_date"] = new ExtractedField
        {
            Name = "effective_date",
            Value = effectiveDate,
            Confidence = effectiveDate is not null ? 0.85m : 0.0m,
            SourceCitation = dateCitation
        };

        // ---- Parties: medical group + covering physicians ----
        var parties = ExtractParties(body);
        fields["parties"] = new ExtractedField
        {
            Name = "parties",
            Value = parties,
            Confidence = parties.Count > 0 ? 0.7m : 0.0m
        };

        // ---- Coverage specialty (e.g. gastroenterology) ----
        var specialty = ExtractSpecialty(body);
        fields["coverage_specialty"] = new ExtractedField
        {
            Name = "coverage_specialty",
            Value = specialty,
            Confidence = specialty is not null ? 0.8m : 0.0m
        };

        // ---- Coverage windows (when call applies) ----
        var windows = ExtractCoverageWindows(body);
        fields["coverage_windows"] = new ExtractedField
        {
            Name = "coverage_windows",
            Value = windows,
            Confidence = windows.Count > 0 ? 0.7m : 0.0m
        };

        // ---- Compensation ----
        var compensation = ExtractCompensation(body);
        fields["compensation"] = new ExtractedField
        {
            Name = "compensation",
            Value = compensation,
            Confidence = (compensation.PerShiftAmount ?? compensation.PerDayAmount ?? compensation.PerMonthAmount) is not null ? 0.8m : 0.0m
        };

        // ---- Term ----
        var (termMonths, termCitation) = ExtractTermMonths(body);
        fields["term_months"] = new ExtractedField
        {
            Name = "term_months",
            Value = termMonths,
            Confidence = termMonths is not null ? 0.8m : 0.0m,
            SourceCitation = termCitation
        };

        // ---- Governing law ----
        var (governingLaw, lawCitation) = ExtractGoverningLaw(body);
        fields["governing_law"] = new ExtractedField
        {
            Name = "governing_law",
            Value = governingLaw,
            Confidence = governingLaw is not null ? 0.85m : 0.0m,
            SourceCitation = lawCitation
        };

        // ---- Signature pass-through ----
        fields["signature_block"] = new ExtractedField
        {
            Name = "signature_block",
            Value = input.SignatureProvider,
            Confidence = isExecuted ? 0.85m : 0.0m
        };

        if (isExecuted) reasons.Add("signature_attached");
        if (windows.Count > 0) reasons.Add($"windows_detected:{windows.Count}");

        return new FieldExtractionResult
        {
            SchemaVersion = SchemaVersion,
            Subtype = "call_coverage",
            Fields = fields,
            IsTemplate = false,
            IsExecuted = isExecuted,
            ReasonCodes = reasons
        };
    }

    private static readonly Regex EffectiveDateRegex = new(
        @"(effective\s+(?:date|as\s+of)?:?\s+|dated\s+|entered\s+into\s+(?:as\s+of\s+)?(?:this\s+)?(?:the\s+)?)" +
        @"(?<value>\d{1,2}(?:st|nd|rd|th)?\s+day\s+of\s+[A-Z][a-z]+,?\s+\d{4}|" +
        @"[A-Z][a-z]+\s+\d{1,2},?\s+\d{4}|\d{1,2}/\d{1,2}/\d{4}|\d{4}-\d{2}-\d{2})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (DateTimeOffset? value, string? citation) ExtractEffectiveDate(string body)
    {
        var match = EffectiveDateRegex.Match(body);
        if (!match.Success) return (null, null);
        var raw = match.Groups["value"].Value;
        var date = RegexHelpers.ParseDate(raw);
        return date is null ? (null, null) : (date, $"effective: {raw}");
    }

    private static readonly Regex MedicalGroupRegex = new(
        @"(?<name>[A-Z][\w&\.\s,'\-]+?(?:Physicians|Medical\s+Group|Associates|P\.A\.|PA|Inc\.|LLC))\s*\(""(?:Medical\s+Group|Group|Practice|Company)""\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CoveringPhysicianRegex = new(
        @"(?:between\s+|and\s+)(?<name>[A-Z][\w\.\s'\-]+?,\s*M\.?D\.?)",
        RegexOptions.Compiled);

    private static List<CallCoverageParty> ExtractParties(string body)
    {
        var list = new List<CallCoverageParty>();

        var groupMatch = MedicalGroupRegex.Match(body);
        if (groupMatch.Success)
        {
            list.Add(new CallCoverageParty(
                Role: "medical_group",
                Name: CleanName(groupMatch.Groups["name"].Value),
                Specialty: null));
        }

        foreach (Match m in CoveringPhysicianRegex.Matches(body))
        {
            var name = CleanName(m.Groups["name"].Value);
            if (!list.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(new CallCoverageParty(
                    Role: "covering_physician",
                    Name: name,
                    Specialty: null));
            }
        }

        return list;
    }

    private static string CleanName(string raw) =>
        raw.Trim().TrimEnd(',', '.', ';').Replace("\n", " ").Replace("\r", " ");

    private static readonly string[] SpecialtyKeywords =
    [
        "gastroenterology", "GI", "cardiology", "orthopedic", "anesthesiology",
        "obstetrics", "gynecology", "pulmonology", "endocrinology", "dermatology",
        "internal medicine", "family medicine", "emergency medicine"
    ];

    private static string? ExtractSpecialty(string body)
    {
        foreach (var kw in SpecialtyKeywords)
        {
            if (body.Contains(kw, StringComparison.OrdinalIgnoreCase))
            {
                return kw;
            }
        }
        return null;
    }

    private static readonly (Regex Pattern, string Type)[] WindowRegexes =
    [
        (new(@"24[\s/x]?7|24\s+hours?\s+(?:a\s+day|per\s+day)|round[-\s]?the[-\s]?clock", RegexOptions.Compiled | RegexOptions.IgnoreCase), "24x7"),
        (new(@"weekend(?:s|\s+call)?|saturday\s+and\s+sunday", RegexOptions.Compiled | RegexOptions.IgnoreCase), "weekend"),
        (new(@"weeknight|weekday\s+evening|monday\s+through\s+friday", RegexOptions.Compiled | RegexOptions.IgnoreCase), "weekday_evenings"),
        (new(@"holiday\s+coverage|holidays?\s+including", RegexOptions.Compiled | RegexOptions.IgnoreCase), "holiday"),
        (new(@"after[-\s]?hours|night\s+call", RegexOptions.Compiled | RegexOptions.IgnoreCase), "after_hours"),
    ];

    private static List<CallCoverageWindow> ExtractCoverageWindows(string body)
    {
        var list = new List<CallCoverageWindow>();
        foreach (var (rx, type) in WindowRegexes)
        {
            var match = rx.Match(body);
            if (match.Success && !list.Any(w => w.CoverageType == type))
            {
                var raw = match.Value.Trim();
                list.Add(new CallCoverageWindow(type, raw.Length > 80 ? raw[..80] + "..." : raw));
            }
        }
        return list;
    }

    private static readonly Regex PerShiftRegex = new(
        @"\$\s?(?<amount>[0-9]{1,3}(?:,[0-9]{3})*(?:\.\d{1,2})?)\s*(?:per|/)\s*(?:shift|call|night|day|hour|month)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static CallCoverageCompensation ExtractCompensation(string body)
    {
        decimal? perShift = null;
        decimal? perDay = null;
        decimal? perMonth = null;
        string? notes = null;

        foreach (Match m in PerShiftRegex.Matches(body))
        {
            var amountRaw = m.Groups["amount"].Value.Replace(",", "");
            if (!decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                continue;
            }
            var unit = m.Value.ToLowerInvariant();
            if (unit.Contains("shift") || unit.Contains("call") || unit.Contains("night"))
            {
                perShift ??= amount;
            }
            else if (unit.Contains("day"))
            {
                perDay ??= amount;
            }
            else if (unit.Contains("month"))
            {
                perMonth ??= amount;
            }
        }

        if (perShift is null && perDay is null && perMonth is null && body.Contains("compensation", StringComparison.OrdinalIgnoreCase))
        {
            notes = "compensation language present but no $ amount captured";
        }

        return new CallCoverageCompensation(perShift, perDay, perMonth, "USD", notes);
    }

    private static readonly Regex TermMonthsRegex = new(
        @"(?:term\s+of|for\s+a\s+term\s+of|for\s+a\s+period\s+of)\s+(?<n>\d+)\s+(?<unit>year|month)s?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (int? months, string? citation) ExtractTermMonths(string body)
    {
        var match = TermMonthsRegex.Match(body);
        if (!match.Success) return (null, null);
        if (!int.TryParse(match.Groups["n"].Value, out var n)) return (null, null);
        var months = match.Groups["unit"].Value.StartsWith("year", StringComparison.OrdinalIgnoreCase) ? n * 12 : n;
        return (months, match.Value);
    }

    private static readonly Regex GoverningLawRegex = new(
        @"governed\s+by\s+(?:and\s+construed\s+in\s+accordance\s+with\s+)?the\s+laws\s+of\s+(?:the\s+)?(?:State\s+of\s+)?(?<value>[A-Z][A-Za-z\s]+?)(?:[\.,]|\s+and|\s+without)",
        RegexOptions.Compiled);

    private static (string? value, string? citation) ExtractGoverningLaw(string body)
    {
        var match = GoverningLawRegex.Match(body);
        if (!match.Success) return (null, null);
        var raw = match.Groups["value"].Value.Trim();
        return (raw, $"governing law: {raw}");
    }
}
