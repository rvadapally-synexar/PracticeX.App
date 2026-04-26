using PracticeX.Domain.Documents;

namespace PracticeX.Discovery.Classification;

/// <summary>
/// Deterministic, rule-based classifier. Production replaces this with the LLM
/// extraction pipeline; the rule version stays as the lightweight pre-pass that
/// hydrates the discovery UI immediately and emits explainable reason codes.
/// </summary>
public sealed class RuleBasedContractClassifier : IDocumentClassifier
{
    public string Version => "rule_v1";

    private static readonly string[] PayerKeywords =
    [
        "payer", "blueshield", "blue shield", "regence", "aetna", "cigna",
        "united healthcare", "uhc", "humana", "anthem", "kaiser", "medicare advantage"
    ];

    private static readonly string[] LeaseKeywords =
    [
        "lease", "sublease", "tenant", "landlord", "premises", "suite "
    ];

    private static readonly string[] VendorKeywords =
    [
        "vendor", "service agreement", "msa", "sow", "olympus", "stryker",
        "boston scientific", "supplies", "equipment"
    ];

    private static readonly string[] EmployeeKeywords =
    [
        "employment", "physician", "comp addendum", "compensation", "noncompete"
    ];

    private static readonly string[] ProcessorKeywords =
    [
        "baa", "business associate", "processor agreement", "data processing"
    ];

    private static readonly string[] AmendmentKeywords =
    [
        "amendment", "addendum", "rider"
    ];

    private static readonly string[] FeeScheduleKeywords =
    [
        "fee schedule", "rate schedule", "exhibit a", "exhibit b", "rate sheet"
    ];

    private static readonly string[] SupportedMimeTypes =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "image/tiff",
        "image/png",
        "image/jpeg"
    ];

    public ClassificationResult Classify(ClassificationInput input)
    {
        var reasons = new List<string>();

        if (!IsSupportedMimeType(input.MimeType, input.FileName))
        {
            reasons.Add(IngestionReasonCodes.UnsupportedMimeType);
            return new ClassificationResult
            {
                CandidateType = DocumentCandidateTypes.Other,
                Confidence = 0.0m,
                ReasonCodes = reasons,
                Status = DocumentCandidateStatus.Skipped
            };
        }

        if (input.SizeBytes is 0)
        {
            reasons.Add(IngestionReasonCodes.EmptyFile);
            return new ClassificationResult
            {
                CandidateType = DocumentCandidateTypes.Unknown,
                Confidence = 0.0m,
                ReasonCodes = reasons,
                Status = DocumentCandidateStatus.Skipped
            };
        }

        var haystack = string.Join(" ",
            input.FileName,
            input.RelativePath ?? string.Empty,
            input.SubjectHint ?? string.Empty,
            input.SenderHint ?? string.Empty,
            input.FolderHint ?? string.Empty)
            .ToLowerInvariant();

        var (type, baseConfidence, typeReasons) = InferType(haystack);
        reasons.AddRange(typeReasons);

        var confidence = baseConfidence;

        if (haystack.Contains("contract") || haystack.Contains("agreement"))
        {
            reasons.Add(IngestionReasonCodes.FilenameContractKeywords);
            confidence = Math.Min(0.99m, confidence + 0.05m);
        }

        foreach (var kw in AmendmentKeywords)
        {
            if (haystack.Contains(kw))
            {
                reasons.Add(IngestionReasonCodes.FilenameAmendment);
                confidence = Math.Min(0.99m, confidence + 0.04m);
                if (type == DocumentCandidateTypes.Unknown)
                {
                    type = DocumentCandidateTypes.Amendment;
                }
                break;
            }
        }

        foreach (var kw in FeeScheduleKeywords)
        {
            if (haystack.Contains(kw))
            {
                reasons.Add(IngestionReasonCodes.FilenameRateSchedule);
                confidence = Math.Min(0.99m, confidence + 0.04m);
                if (type == DocumentCandidateTypes.Unknown)
                {
                    type = DocumentCandidateTypes.FeeSchedule;
                }
                break;
            }
        }

        if (input.FolderHint?.Contains("payer", StringComparison.OrdinalIgnoreCase) == true)
        {
            reasons.Add(IngestionReasonCodes.FolderHintPayer);
            confidence = Math.Min(0.99m, confidence + 0.03m);
        }
        if (input.FolderHint?.Contains("lease", StringComparison.OrdinalIgnoreCase) == true)
        {
            reasons.Add(IngestionReasonCodes.FolderHintLease);
            if (type == DocumentCandidateTypes.Unknown)
            {
                type = DocumentCandidateTypes.Lease;
                confidence = Math.Max(confidence, 0.6m);
            }
        }

        foreach (var hint in input.Hints)
        {
            reasons.Add(hint);
        }

        if (type == DocumentCandidateTypes.Unknown)
        {
            reasons.Add(IngestionReasonCodes.AmbiguousType);
        }
        else
        {
            reasons.Add(IngestionReasonCodes.LikelyContract);
        }

        var status = confidence >= 0.55m
            ? DocumentCandidateStatus.PendingReview
            : DocumentCandidateStatus.Candidate;

        var counterparty = ExtractCounterpartyHint(haystack);

        return new ClassificationResult
        {
            CandidateType = type,
            Confidence = decimal.Round(confidence, 4),
            ReasonCodes = reasons,
            Status = status,
            CounterpartyHint = counterparty
        };
    }

    private static (string type, decimal confidence, IEnumerable<string> reasons) InferType(string haystack)
    {
        var reasons = new List<string>();

        foreach (var kw in PayerKeywords)
        {
            if (haystack.Contains(kw))
            {
                return (DocumentCandidateTypes.PayerContract, 0.78m, reasons);
            }
        }
        foreach (var kw in LeaseKeywords)
        {
            if (haystack.Contains(kw))
            {
                return (DocumentCandidateTypes.Lease, 0.74m, reasons);
            }
        }
        foreach (var kw in EmployeeKeywords)
        {
            if (haystack.Contains(kw))
            {
                return (DocumentCandidateTypes.EmployeeAgreement, 0.7m, reasons);
            }
        }
        foreach (var kw in ProcessorKeywords)
        {
            if (haystack.Contains(kw))
            {
                return (DocumentCandidateTypes.ProcessorAgreement, 0.7m, reasons);
            }
        }
        foreach (var kw in VendorKeywords)
        {
            if (haystack.Contains(kw))
            {
                return (DocumentCandidateTypes.VendorContract, 0.7m, reasons);
            }
        }

        return (DocumentCandidateTypes.Unknown, 0.4m, reasons);
    }

    private static bool IsSupportedMimeType(string mimeType, string fileName)
    {
        if (SupportedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".txt" or ".tif" or ".tiff" or ".png" or ".jpg" or ".jpeg";
    }

    private static string? ExtractCounterpartyHint(string haystack)
    {
        foreach (var kw in PayerKeywords.Concat(VendorKeywords))
        {
            var idx = haystack.IndexOf(kw, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                return kw;
            }
        }
        return null;
    }
}
