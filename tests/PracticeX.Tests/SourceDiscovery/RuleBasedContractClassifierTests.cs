using PracticeX.Discovery.Classification;
using PracticeX.Domain.Documents;

namespace PracticeX.Tests.SourceDiscovery;

public class RuleBasedContractClassifierTests
{
    private readonly RuleBasedContractClassifier _classifier = new();

    [Fact]
    public void Classify_PayerKeywordInFilename_ClassifiesAsPayerContract()
    {
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = "Regence BlueShield Amendment 3.pdf",
            MimeType = "application/pdf",
            SizeBytes = 1024 * 12
        });

        Assert.Equal(DocumentCandidateTypes.PayerContract, result.CandidateType);
        Assert.True(result.Confidence > 0.7m);
        Assert.Contains(IngestionReasonCodes.LikelyContract, result.ReasonCodes);
        Assert.Contains(IngestionReasonCodes.FilenameAmendment, result.ReasonCodes);
    }

    [Fact]
    public void Classify_FolderHintLease_DefaultsToLease()
    {
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = "Suite 310 main agreement.pdf",
            MimeType = "application/pdf",
            FolderHint = "/payer/lease/suite-310",
            SizeBytes = 4096
        });

        Assert.Equal(DocumentCandidateTypes.Lease, result.CandidateType);
        Assert.Contains(IngestionReasonCodes.FolderHintLease, result.ReasonCodes);
    }

    [Fact]
    public void Classify_UnsupportedMimeType_Skipped()
    {
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = "video.mp4",
            MimeType = "video/mp4",
            SizeBytes = 100_000_000
        });

        Assert.Equal(DocumentCandidateStatus.Skipped, result.Status);
        Assert.Contains(IngestionReasonCodes.UnsupportedMimeType, result.ReasonCodes);
    }

    [Fact]
    public void Classify_EmptyFile_SkippedWithReason()
    {
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = "empty.pdf",
            MimeType = "application/pdf",
            SizeBytes = 0
        });

        Assert.Equal(DocumentCandidateStatus.Skipped, result.Status);
        Assert.Contains(IngestionReasonCodes.EmptyFile, result.ReasonCodes);
    }

    [Fact]
    public void Classify_AmbiguousFilename_StaysAtCandidateAndFlagsAmbiguity()
    {
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = "scan_001.pdf",
            MimeType = "application/pdf",
            SizeBytes = 32_000
        });

        Assert.Equal(DocumentCandidateTypes.Unknown, result.CandidateType);
        Assert.Contains(IngestionReasonCodes.AmbiguousType, result.ReasonCodes);
        Assert.True(result.Confidence < 0.55m);
        Assert.Equal(DocumentCandidateStatus.Candidate, result.Status);
    }

    [Fact]
    public void Classify_VendorKeyword_ClassifiesAsServiceAgreement()
    {
        // Slice 8: ServiceAgreement is a more specific bucket than the legacy
        // VendorContract for "X Service Agreement" filenames. Olympus is a
        // vendor, so the counterparty hint is still populated.
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = "Olympus Service Agreement Renewal.docx",
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            SizeBytes = 22_000
        });

        Assert.Equal(DocumentCandidateTypes.ServiceAgreement, result.CandidateType);
        Assert.Equal(DocumentCandidateStatus.PendingReview, result.Status);
        Assert.NotNull(result.CounterpartyHint);
    }

    [Theory]
    [InlineData("CONE_2022_Bylaws_R&R.pdf",                                       "bylaws")]
    [InlineData("EagleGICallcoverageAgreement.pdf",                               "call_coverage_agreement")]
    [InlineData("01_call_coverage.pdf",                                           "call_coverage_agreement")]
    [InlineData("signed NDA 1103 North Elm Street, Greens.pdf",                  "nda")]
    [InlineData("CONFIDENTIAL- Signed NDA. .pdf",                                 "nda")]
    [InlineData("Mutual Non-Disclosure Agreement.pdf",                           "nda")]
    [InlineData("LOI_Eagle Renewal_1002 N. Church Street.docx",                  "lease_loi")]
    [InlineData("Eagle -PMC-LOI Lease.docx",                                      "lease_loi")]
    [InlineData("EEC Office 4th Lease Amemdment - CD103117.pdf",                 "lease_amendment")]
    [InlineData("Eighth Amendment to Lease Agreement.pdf",                       "lease_amendment")]
    [InlineData("AP Labs Service Agreement - Eagle MDS.pdf",                     "service_agreement")]
    [InlineData("ReleaseNotes.txt",                                               "unknown")] // false-positive guard: "release" must NOT match "lease"
    public void Classify_NewSlice8Types_RoutedCorrectly(string fileName, string expected)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
        var result = _classifier.Classify(new ClassificationInput
        {
            FileName = fileName,
            MimeType = mime,
            SizeBytes = 4096
        });
        Assert.Equal(expected, result.CandidateType);
    }
}
