using System.Globalization;
using System.Text.RegularExpressions;
using PracticeX.Discovery.FieldExtraction.Helpers;
using PracticeX.Discovery.Schemas;

namespace PracticeX.Discovery.FieldExtraction;

/// <summary>
/// v1 Lease-family field extractor — regex-only scaffold over the layout text
/// that comes back from local PDF/DOCX extraction or Azure Document Intelligence
/// OCR. Detects the subtype (master lease / amendment / LOI), pulls the
/// landlord+tenant, premises (address, suite, sqft), effective date, governing
/// law, and amendment-of metadata. LLM-quality extraction is v2.
///
/// Eagle GI's lease portfolio drove every regex below; the canonical pattern is
/// "by and between [LANDLORD] ... and [TENANT]" with premises described as
/// "approximately X rentable square feet ... Suite Y in the building located
/// at Z".
/// </summary>
public sealed class LeaseExtractor : IContractFieldExtractor
{
    public string Name => "lease-extractor-v1";
    public string SchemaVersion => LeaseSchemaV1Constants.Version;

    public bool CanExtract(string subtypeOrCandidateType)
    {
        if (string.IsNullOrWhiteSpace(subtypeOrCandidateType)) return false;
        var key = subtypeOrCandidateType.Trim().ToLowerInvariant();
        return key is "lease"
            or "lease_amendment"
            or "lease_loi"
            or "sublease"
            or "master_lease";
    }

    public FieldExtractionResult Extract(FieldExtractionInput input)
    {
        var fileName = input.FileName ?? string.Empty;
        var body = input.FullText ?? string.Empty;
        var subtype = DetectSubtype(fileName, body);

        var fields = new Dictionary<string, ExtractedField>(StringComparer.OrdinalIgnoreCase);
        var reasons = new List<string> { "lease_extractor_v1", $"subtype_detected:{subtype}" };
        var isTemplate = false;
        var isExecuted = false;

        // ---- Effective date ----
        var (effectiveDate, effectiveCitation) = ExtractEffectiveDate(body);
        fields["effective_date"] = new ExtractedField
        {
            Name = "effective_date",
            Value = effectiveDate,
            Confidence = effectiveDate is not null ? 0.85m : 0.0m,
            SourceCitation = effectiveCitation
        };

        // ---- Landlord / Tenant ----
        var (landlord, tenant, partyCitation) = ExtractParties(body);
        fields["landlord"] = new ExtractedField
        {
            Name = "landlord",
            Value = landlord,
            Confidence = landlord is not null ? 0.7m : 0.0m,
            SourceCitation = partyCitation
        };
        fields["tenant"] = new ExtractedField
        {
            Name = "tenant",
            Value = tenant,
            Confidence = tenant is not null ? 0.7m : 0.0m,
            SourceCitation = partyCitation
        };

        // ---- Premises (one or more) ----
        var premises = ExtractPremises(body);
        fields["premises"] = new ExtractedField
        {
            Name = "premises",
            Value = premises,
            Confidence = premises.Count > 0 ? 0.8m : 0.0m
        };

        // ---- Total rentable sqft (sum across premises) ----
        var totalSqft = premises.Where(p => p.RentableSquareFeet.HasValue).Sum(p => p.RentableSquareFeet!.Value);
        fields["total_rentable_sqft"] = new ExtractedField
        {
            Name = "total_rentable_sqft",
            Value = totalSqft > 0m ? totalSqft : null,
            Confidence = totalSqft > 0m ? 0.85m : 0.0m
        };

        // ---- Rent (base + escalation) ----
        var rent = ExtractRent(body);
        fields["rent"] = new ExtractedField
        {
            Name = "rent",
            Value = rent,
            Confidence = rent.BaseRent is not null ? 0.7m : (rent.DeferredFlag ? 0.5m : 0.0m)
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

        // ---- Amendment ref (only meaningful for amendments) ----
        if (subtype == LeaseSchemaV1Constants.Subtypes.LeaseAmendment)
        {
            var amendmentRef = ExtractAmendmentRef(fileName, body);
            fields["amends"] = new ExtractedField
            {
                Name = "amends",
                Value = amendmentRef,
                Confidence = amendmentRef.ParentDocumentTitle is not null ? 0.7m : 0.3m
            };
        }

        // ---- Signature pass-through ----
        if (!string.IsNullOrWhiteSpace(input.SignatureProvider))
        {
            isExecuted = true;
        }
        fields["signature_block"] = new ExtractedField
        {
            Name = "signature_block",
            Value = input.SignatureProvider,
            Confidence = isExecuted ? 0.85m : 0.0m
        };

        if (isTemplate) reasons.Add("template_placeholders_present");
        if (isExecuted) reasons.Add("signature_attached");
        if (premises.Count > 1) reasons.Add($"multi_suite:{premises.Count}");

        return new FieldExtractionResult
        {
            SchemaVersion = SchemaVersion,
            Subtype = subtype,
            Fields = fields,
            IsTemplate = isTemplate,
            IsExecuted = isExecuted,
            ReasonCodes = reasons
        };
    }

    private static string DetectSubtype(string fileName, string body)
    {
        var fn = fileName.ToLowerInvariant();
        if (Regex.IsMatch(fn, @"\bloi\b") || body.Contains("LETTER OF INTENT", StringComparison.OrdinalIgnoreCase))
            return LeaseSchemaV1Constants.Subtypes.LeaseLoi;
        if (fn.Contains("amend") || body.Contains("AMENDMENT TO LEASE", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(body, @"(FIRST|SECOND|THIRD|FOURTH|FIFTH|SIXTH|SEVENTH|EIGHTH|NINTH|TENTH)\s+AMENDMENT", RegexOptions.IgnoreCase))
            return LeaseSchemaV1Constants.Subtypes.LeaseAmendment;
        if (fn.Contains("sublease") || body.Contains("SUBLEASE", StringComparison.OrdinalIgnoreCase))
            return LeaseSchemaV1Constants.Subtypes.SubLease;
        return LeaseSchemaV1Constants.Subtypes.MasterLease;
    }

    // -------- Effective date --------
    // Eagle GI patterns:
    //   "entered into as of the 16th day of December, 2015"
    //   "Date: August 3/2005"
    //   "as of April 5, 2024"
    private static readonly Regex EffectiveDateRegex1 = new(
        @"as of (the )?(?<day>\d{1,2})(st|nd|rd|th)? day of (?<month>[A-Z][a-z]+),?\s+(?<year>\d{4})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EffectiveDateRegex2 = new(
        @"as of (?<month>[A-Z][a-z]+)\s+(?<day>\d{1,2}),?\s+(?<year>\d{4})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EffectiveDateRegex3 = new(
        @"\bDate:?\s+(?<value>[A-Z][a-z]+\s+\d{1,2}[,/]?\s*\d{4}|\d{1,2}/\d{1,2}/\d{4})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (DateTimeOffset? value, string? citation) ExtractEffectiveDate(string body)
    {
        var m1 = EffectiveDateRegex1.Match(body);
        if (m1.Success)
        {
            var raw = $"{m1.Groups["month"].Value} {m1.Groups["day"].Value}, {m1.Groups["year"].Value}";
            var date = RegexHelpers.ParseDate(raw);
            if (date is not null) return (date, $"effective: {m1.Value}");
        }

        var m2 = EffectiveDateRegex2.Match(body);
        if (m2.Success)
        {
            var raw = $"{m2.Groups["month"].Value} {m2.Groups["day"].Value}, {m2.Groups["year"].Value}";
            var date = RegexHelpers.ParseDate(raw);
            if (date is not null) return (date, $"effective: {m2.Value}");
        }

        var m3 = EffectiveDateRegex3.Match(body);
        if (m3.Success)
        {
            var date = RegexHelpers.ParseDate(m3.Groups["value"].Value);
            if (date is not null) return (date, $"effective: {m3.Value}");
        }

        return (null, null);
    }

    // -------- Parties (Landlord / Tenant) --------
    // Eagle GI patterns:
    //   "by and between [LANDLORD], a Delaware limited liability company ... and [TENANT], a North Carolina professional association"
    //   "Landlord: PMC East, LLC c/o Brown Investment Properties"
    //   "Tenant: Eagle Physicians and Associates, P.A."
    private static readonly Regex LandlordLabeledRegex = new(
        @"Landlord:?\s+(?<name>[A-Z][\w&\.\s,'\-/]+?)(\s+c/o\s+|\s*Tenant:|\r|\n)",
        RegexOptions.Compiled);

    private static readonly Regex TenantLabeledRegex = new(
        @"Tenant:?\s+(?<name>[A-Z][\w&\.\s,'\-/]+?)(\s+\(""|\s*Guarantor:|\r|\n)",
        RegexOptions.Compiled);

    // "by and between X and Y" — captures up to the next entity-shape break
    private static readonly Regex BetweenPartiesRegex = new(
        @"between\s+(?<a>[A-Z][\w&\.\s,'\-/]+?)(\s*\(""(?<aRole>[^""]+)""\))?\s+and\s+(?<b>[A-Z][\w&\.\s,'\-/]+?)(\s*\(""(?<bRole>[^""]+)""\))?[\.\,]",
        RegexOptions.Compiled);

    private static (string? landlord, string? tenant, string? citation) ExtractParties(string body)
    {
        var landlordMatch = LandlordLabeledRegex.Match(body);
        var tenantMatch = TenantLabeledRegex.Match(body);
        if (landlordMatch.Success && tenantMatch.Success)
        {
            return (
                CleanName(landlordMatch.Groups["name"].Value),
                CleanName(tenantMatch.Groups["name"].Value),
                "labeled landlord/tenant");
        }

        var between = BetweenPartiesRegex.Match(body);
        if (between.Success)
        {
            return (
                CleanName(between.Groups["a"].Value),
                CleanName(between.Groups["b"].Value),
                $"by and between: {between.Value[..Math.Min(80, between.Value.Length)]}...");
        }

        return (null, null, null);
    }

    private static string CleanName(string raw) =>
        raw.Trim().TrimEnd(',', '.', ';').Replace("\n", " ").Replace("\r", " ");

    // -------- Premises (address + suite + sqft) --------
    // Eagle GI pattern:
    //   "approximately 7,622 rentable square feet of space commonly known as Suite 002 ... building ... located at 1002 North Church Street in Greensboro, North Carolina"
    private static readonly Regex SqftSuiteRegex = new(
        @"approximately\s+(?<sqft>[\d,]+)\s+rentable\s+square\s+feet[^.]*?Suite\s+(?<suite>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AddressRegex = new(
        @"(located\s+at|building\s+located\s+at)\s+(?<street>\d+\s+[A-Z][A-Za-z0-9\.\s]+(Street|Road|Avenue|Drive|Boulevard|Way|Place|Lane|Court|Plaza|Parkway|St\.?|Rd\.?|Ave\.?|Dr\.?|Blvd\.?))(\s+in\s+)?(?<city>[A-Z][A-Za-z\.\s]+)?,?\s*(?<state>North Carolina|NC|California|CA|Texas|TX|Delaware|DE|New York|NY|[A-Z]{2})?",
        RegexOptions.Compiled);

    // "1002 North Church Street, Greensboro, North Carolina" pattern (headers / labels)
    private static readonly Regex AddressLabeledRegex = new(
        @"(?<street>\d+\s+[A-Z][A-Za-z0-9\.\s]+?(Street|Road|Avenue|Drive|Boulevard|Way|Place|Lane|Court|Plaza|St\.?|Rd\.?|Ave\.?|Dr\.?|Blvd\.?))[,\s]+(?<city>[A-Z][A-Za-z]+(\s+[A-Z][A-Za-z]+)?)\s*,?\s*(?<state>North Carolina|NC|California|CA|Texas|TX|Delaware|DE|New York|NY)",
        RegexOptions.Compiled);

    private static List<LeasePremises> ExtractPremises(string body)
    {
        var list = new List<LeasePremises>();

        // First pass: every "approximately N rentable square feet ... Suite M" pairing.
        foreach (Match m in SqftSuiteRegex.Matches(body))
        {
            var sqftRaw = m.Groups["sqft"].Value.Replace(",", "");
            decimal? sqft = decimal.TryParse(sqftRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var s) ? s : null;
            var suite = m.Groups["suite"].Value.Trim();

            list.Add(new LeasePremises(
                StreetAddress: null,
                City: null,
                State: null,
                PostalCode: null,
                BuildingName: null,
                Suite: suite,
                RentableSquareFeet: sqft));
        }

        // Resolve the building address once (single building per lease in 99% of cases).
        var addrMatch = AddressRegex.Match(body);
        var addrLabeledMatch = AddressLabeledRegex.Match(body);
        var address = addrMatch.Success ? CleanName(addrMatch.Groups["street"].Value) :
                      addrLabeledMatch.Success ? CleanName(addrLabeledMatch.Groups["street"].Value) : null;
        var city = addrMatch.Success && addrMatch.Groups["city"].Success ? CleanName(addrMatch.Groups["city"].Value) :
                   addrLabeledMatch.Success ? CleanName(addrLabeledMatch.Groups["city"].Value) : null;
        var state = addrMatch.Success && addrMatch.Groups["state"].Success ? addrMatch.Groups["state"].Value :
                    addrLabeledMatch.Success ? addrLabeledMatch.Groups["state"].Value : null;

        if (address is not null)
        {
            if (list.Count == 0)
            {
                // No suite/sqft info found — emit a single address-only record.
                list.Add(new LeasePremises(address, city, state, null, null, null, null));
            }
            else
            {
                // Backfill the address into every suite record.
                for (var i = 0; i < list.Count; i++)
                {
                    list[i] = list[i] with { StreetAddress = address, City = city, State = state };
                }
            }
        }

        return list;
    }

    // -------- Rent --------
    private static readonly Regex BaseRentRegex = new(
        @"(?<lead>base\s+rent|monthly\s+rent|annual\s+rent|rent\s+of)[:\s]+\$\s?(?<amount>[0-9]{1,3}(,[0-9]{3})*(\.\d{1,2})?)\s*(per\s+(?<period>month|year|annum|sq\.?\s*ft))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static LeaseRent ExtractRent(string body)
    {
        MoneyRecord? baseRent = null;
        string? escalation = null;
        var deferred = body.Contains("deferred rent", StringComparison.OrdinalIgnoreCase) ||
                       body.Contains("Defered Rent", StringComparison.OrdinalIgnoreCase);

        var match = BaseRentRegex.Match(body);
        if (match.Success)
        {
            var amountRaw = match.Groups["amount"].Value.Replace(",", "");
            if (decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                var period = match.Groups["period"].Success ? match.Groups["period"].Value.ToLowerInvariant() : "month";
                if (period.StartsWith("annum")) period = "year";
                baseRent = new MoneyRecord(amount, "USD", period);
            }
        }

        if (Regex.IsMatch(body, @"\d+(\.\d+)?\s*%\s+annual\s+(increase|escalation)", RegexOptions.IgnoreCase))
        {
            escalation = "annual_percent";
        }
        else if (body.Contains("CPI", StringComparison.Ordinal))
        {
            escalation = "cpi";
        }

        return new LeaseRent(baseRent, escalation, null, deferred);
    }

    // -------- Governing law --------
    private static readonly Regex GoverningLawRegex = new(
        @"governed by (and construed in accordance with )?the laws of (the )?(State of )?(?<value>[A-Z][A-Za-z\s]+?)(\.|,|\sand|\swithout)",
        RegexOptions.Compiled);

    private static (string? value, string? citation) ExtractGoverningLaw(string body)
    {
        var match = GoverningLawRegex.Match(body);
        if (!match.Success) return (null, null);
        var raw = match.Groups["value"].Value.Trim();
        return (raw, $"governing law: {raw}");
    }

    // -------- Amendment ref --------
    private static readonly Regex AmendmentNumberRegex = new(
        @"(?<word>FIRST|SECOND|THIRD|FOURTH|FIFTH|SIXTH|SEVENTH|EIGHTH|NINTH|TENTH)\s+AMENDMENT",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AmendmentNumberDigitRegex = new(
        @"(?<n>\d+)(st|nd|rd|th)\s+amendment|amendment\s+(no\.?\s+)?(?<n2>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParentLeaseRegex = new(
        @"(Lease Agreement|lease agreement) dated\s+(?<date>[A-Z][a-z]+\s+\d{1,2},?\s+\d{4}|\d{1,2}/\d{1,2}/\d{4})",
        RegexOptions.Compiled);

    private static LeaseAmendmentRef ExtractAmendmentRef(string fileName, string body)
    {
        int? amendmentNumber = null;

        var word = AmendmentNumberRegex.Match(body);
        if (word.Success)
        {
            amendmentNumber = word.Groups["word"].Value.ToUpperInvariant() switch
            {
                "FIRST" => 1,
                "SECOND" => 2,
                "THIRD" => 3,
                "FOURTH" => 4,
                "FIFTH" => 5,
                "SIXTH" => 6,
                "SEVENTH" => 7,
                "EIGHTH" => 8,
                "NINTH" => 9,
                "TENTH" => 10,
                _ => null
            };
        }

        if (amendmentNumber is null)
        {
            var digit = AmendmentNumberDigitRegex.Match(body);
            if (digit.Success)
            {
                var nRaw = digit.Groups["n"].Success ? digit.Groups["n"].Value : digit.Groups["n2"].Value;
                if (int.TryParse(nRaw, out var n)) amendmentNumber = n;
            }
        }

        // Filename fallback: "EEC Office 4th Lease Amemdment" -> 4
        if (amendmentNumber is null)
        {
            var fnDigit = Regex.Match(fileName, @"(?<n>\d+)(st|nd|rd|th)?\s*(lease\s+)?(amend|addendum)", RegexOptions.IgnoreCase);
            if (fnDigit.Success && int.TryParse(fnDigit.Groups["n"].Value, out var n)) amendmentNumber = n;
        }

        var parent = ParentLeaseRegex.Match(body);
        DateTimeOffset? parentDate = null;
        string? parentTitle = null;
        if (parent.Success)
        {
            parentDate = RegexHelpers.ParseDate(parent.Groups["date"].Value);
            parentTitle = parent.Value;
        }

        return new LeaseAmendmentRef(parentDate, parentTitle, amendmentNumber);
    }
}
