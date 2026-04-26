using System.Globalization;
using System.Text.RegularExpressions;

namespace PracticeX.Discovery.FieldExtraction.Helpers;

/// <summary>
/// Shared regexes + parsers for the v1 regex-only field extractors.
/// LLM-quality parsing is v2 — these helpers exist to make the v1
/// scaffolds short and consistent.
/// </summary>
public static class RegexHelpers
{
    // "April 11, 2026" / "April 11 2026"
    public static readonly Regex DateLong = new(
        @"\b(?<month>January|February|March|April|May|June|July|August|September|October|November|December)\s+(?<day>\d{1,2}),?\s+(?<year>\d{4})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // "4/11/2026" — US convention
    public static readonly Regex DateSlash = new(
        @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{4})\b",
        RegexOptions.Compiled);

    // ISO "2026-04-11"
    public static readonly Regex DateIso = new(
        @"\b(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\b",
        RegexOptions.Compiled);

    // "$150,000" / "$150,000.00"
    public static readonly Regex Money = new(
        @"\$\s?(?<amount>[0-9]{1,3}(,[0-9]{3})*(\.\d{2})?|\d+(\.\d{2})?)",
        RegexOptions.Compiled);

    // "1.00%" / "3%"
    public static readonly Regex Percent = new(
        @"(?<value>\d+(\.\d+)?)\s*%",
        RegexOptions.Compiled);

    // Docusign envelope IDs are GUIDs in the document.
    public static readonly Regex DocusignEnvelope = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled);

    public static readonly Regex PlaceholderBlank = new(
        @"_{3,}|\[Your\s+\w+\]|\[(state|date|name)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] LongFormats =
    {
        "MMMM d, yyyy", "MMMM d yyyy", "MMMM dd, yyyy", "MMMM dd yyyy"
    };

    public static DateTimeOffset? ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var trimmed = text.Trim();

        var iso = DateIso.Match(trimmed);
        if (iso.Success && DateTime.TryParseExact(iso.Value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var isoDate))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(isoDate, DateTimeKind.Utc));
        }

        var slash = DateSlash.Match(trimmed);
        if (slash.Success && DateTime.TryParseExact(slash.Value, new[] { "M/d/yyyy", "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var slashDate))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(slashDate, DateTimeKind.Utc));
        }

        var lng = DateLong.Match(trimmed);
        if (lng.Success)
        {
            var month = lng.Groups["month"].Value;
            var day = lng.Groups["day"].Value;
            var year = lng.Groups["year"].Value;
            var canonical = $"{month} {day}, {year}";
            if (DateTime.TryParseExact(canonical, LongFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var lngDate))
            {
                return new DateTimeOffset(DateTime.SpecifyKind(lngDate, DateTimeKind.Utc));
            }
        }

        return null;
    }

    public static decimal? ParseMoney(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = Money.Match(text);
        if (!match.Success) return null;
        var raw = match.Groups["amount"].Value.Replace(",", "");
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static decimal? ParsePercent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = Percent.Match(text);
        if (!match.Success) return null;
        var raw = match.Groups["value"].Value;
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static bool LooksLikePlaceholder(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return PlaceholderBlank.IsMatch(text);
    }
}
