using PracticeX.Discovery.FieldExtraction;
using PracticeX.Discovery.Schemas;
using PracticeX.Discovery.TextExtraction;

namespace PracticeX.Tests.SourceDiscovery;

public class EmploymentExtractorTests
{
    private static FieldExtractionInput Build(
        string body,
        string fileName = "doc.pdf",
        string? signatureProvider = null,
        string? envelopeId = null,
        IReadOnlyList<ExtractedHeading>? headings = null)
        => new()
        {
            FullText = body,
            Pages = new[] { new ExtractedPage(1, body) },
            Headings = headings ?? Array.Empty<ExtractedHeading>(),
            FileName = fileName,
            SignatureProvider = signatureProvider,
            DocusignEnvelopeId = envelopeId
        };

    [Fact]
    public void CanExtract_Subtypes()
    {
        var x = new EmploymentExtractor();
        Assert.True(x.CanExtract(EmploymentSchemaV1Constants.Subtypes.OfferLetter));
        Assert.True(x.CanExtract(EmploymentSchemaV1Constants.Subtypes.EngagementLetter));
        Assert.True(x.CanExtract(EmploymentSchemaV1Constants.Subtypes.AdvisorAgreement));
        Assert.True(x.CanExtract(EmploymentSchemaV1Constants.Subtypes.Ciia));
        Assert.True(x.CanExtract(EmploymentSchemaV1Constants.Subtypes.PhiAgreement));
        Assert.True(x.CanExtract("employee_agreement"));
        Assert.False(x.CanExtract("payer_contract"));
        Assert.False(x.CanExtract("lease"));
        Assert.False(x.CanExtract(""));
    }

    [Fact]
    public void Extract_OfferLetter_DetectsSubtype()
    {
        var body = """
            We are pleased to extend this offer letter to you for the position of Senior Engineer.
            Effective Date: April 11, 2026.
            Your annual base salary of $150,000 per year will be paid bi-weekly.
            This agreement is governed by the laws of the State of Delaware.
            """;

        var x = new EmploymentExtractor();
        var result = x.Extract(Build(body, fileName: "Acme_Offer_Letter.pdf"));

        Assert.Equal(EmploymentSchemaV1Constants.Version, result.SchemaVersion);
        Assert.Equal(EmploymentSchemaV1Constants.Subtypes.OfferLetter, result.Subtype);
        Assert.Contains("subtype_detected:offer_letter", result.ReasonCodes);
        Assert.Contains("employment_extractor_v1", result.ReasonCodes);

        var salary = Assert.IsType<MoneyRecord>(result.Fields["base_salary"].Value);
        Assert.Equal(150000m, salary.Amount);
        Assert.Equal("USD", salary.Currency);
    }

    [Fact]
    public void Extract_AdvisorAgreement_GoldSampleShape()
    {
        var body = """
            ADVISOR AGREEMENT
            Effective Date: April 11, 2026
            This Advisory Services agreement is between Acme Inc. ("Company") and Jane Doe ("Advisor").
            The Company grants Advisor 1.00% of fully diluted equity, with a 24-month vest and 6-month cliff.
            Additionally, the Advisor shall earn a growth grant of 1.00% per $1M Net New ARR, capped at 3.00%,
            with pro-rata 0.10% per $100K Net New ARR.
            This agreement is governed by the laws of the State of Delaware.
            """;

        var x = new EmploymentExtractor();
        var result = x.Extract(Build(body, fileName: "Sample6_Advisor_Agreement.pdf"));

        Assert.Equal(EmploymentSchemaV1Constants.Subtypes.AdvisorAgreement, result.Subtype);

        var grants = Assert.IsType<List<EquityGrant>>(result.Fields["equity_grants"].Value);
        Assert.Equal(2, grants.Count);

        var core = grants.Single(g => g.Type == "core_advisory");
        Assert.Equal(1.00m, core.PercentageOfFullyDiluted);
        Assert.Equal(24, core.Vesting.DurationMonths);
        Assert.Equal(6, core.Vesting.CliffMonths);

        var growth = grants.Single(g => g.Type == "growth");
        Assert.Equal(3.00m, growth.CapPercentage);
        Assert.NotNull(growth.ProRata);
        Assert.Equal(0.001m, growth.ProRata!.AmountPerUnit);
        Assert.Contains("100K", growth.ProRata.UnitDescription);

        var effective = (DateTimeOffset)result.Fields["effective_date"].Value!;
        Assert.Equal(2026, effective.Year);
        Assert.Equal(4, effective.Month);
        Assert.Equal(11, effective.Day);

        Assert.Equal("Delaware", result.Fields["governing_law"].Value);
    }

    [Fact]
    public void Extract_TemplateWithPlaceholders_FlagsTemplate()
    {
        var body = """
            ADVISOR AGREEMENT
            Effective Date: ____________
            This Advisory Services agreement is governed by the laws of [Your State].
            """;

        var x = new EmploymentExtractor();
        var result = x.Extract(Build(body, fileName: "Advisor_Template.docx"));

        Assert.True(result.IsTemplate);
        Assert.Contains("template_placeholders_present", result.ReasonCodes);
        Assert.Null(result.Fields["effective_date"].Value);
        Assert.Null(result.Fields["governing_law"].Value);
    }

    [Fact]
    public void Extract_DocusignEnvelopeFromInput_AttachesSignature()
    {
        var body = """
            ADVISOR AGREEMENT
            Effective Date: April 11, 2026
            This Advisory Services agreement is governed by the laws of Delaware.
            """;

        var x = new EmploymentExtractor();
        var result = x.Extract(Build(
            body,
            fileName: "advisor_signed.pdf",
            signatureProvider: "docusign",
            envelopeId: "11111111-2222-3333-4444-555555555555"));

        Assert.True(result.IsExecuted);
        Assert.Contains("signature_attached", result.ReasonCodes);
        var sigs = Assert.IsType<List<SignatureRecord>>(result.Fields["signature_block"].Value);
        var sig = Assert.Single(sigs);
        Assert.Equal("docusign", sig.SignatureProvider);
        Assert.Equal("11111111-2222-3333-4444-555555555555", sig.EnvelopeId);
    }

    [Fact]
    public void Extract_PhiAgreement_DefaultBreachWindow_SixtyDays()
    {
        var body = """
            BUSINESS ASSOCIATE AGREEMENT
            Effective Date: April 11, 2026
            This agreement covers Protected Health Information under HIPAA.
            Acme Health ("Covered Entity") and Vendor LLC ("Business Associate") agree to the terms below.
            """;

        var x = new EmploymentExtractor();
        var result = x.Extract(Build(body, fileName: "Acme_PHI_BAA.pdf"));

        Assert.Equal(EmploymentSchemaV1Constants.Subtypes.PhiAgreement, result.Subtype);
        var breach = Assert.IsType<Dictionary<string, object?>>(result.Fields["breach_notification"].Value);
        Assert.Equal(60, breach["window_days"]);
    }
}
