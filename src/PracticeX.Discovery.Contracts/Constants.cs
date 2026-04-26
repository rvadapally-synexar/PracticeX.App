namespace PracticeX.Discovery.Contracts;

/// <summary>
/// Reason codes emitted by the classifier and validity inspector. These strings
/// are the wire-level vocabulary — both the API and the desktop agent surface
/// them verbatim in UI ("Why this matched"), so they are part of the contract.
/// </summary>
public static class DiscoveryReasonCodes
{
    // Validity / mime
    public const string UnsupportedMimeType = "unsupported_mime_type";
    public const string EmptyFile = "empty_file";
    public const string ExceedsSizeLimit = "exceeds_size_limit";
    public const string DuplicateContent = "duplicate_content";

    // Classifier
    public const string LikelyContract = "likely_contract";
    public const string AmbiguousType = "ambiguous_type";
    public const string FilenameContractKeywords = "filename_contract_keywords";
    public const string FilenameAmendment = "filename_amendment";
    public const string FilenameRateSchedule = "filename_rate_schedule";
    public const string FolderHintPayer = "folder_hint_payer";
    public const string FolderHintLease = "folder_hint_lease";

    // Outlook-sourced
    public const string OutlookSenderDomain = "outlook_sender_domain";
    public const string OutlookSubjectKeywords = "outlook_subject_keywords";

    // Validity inspector — file-shape verdicts
    public const string ValidPdf = "valid_pdf";
    public const string CorruptPdf = "corrupt_pdf";
    public const string EncryptedPdf = "encrypted_pdf";
    public const string PdfHasTextLayer = "pdf_has_text_layer";
    public const string PdfScanned = "pdf_scanned";
    public const string ValidOfficeContainer = "valid_office_container";
    public const string CorruptOfficeContainer = "corrupt_office_container";
    public const string ValidText = "valid_text";
    public const string UnsupportedContainer = "unsupported_container";

    // Signature detection — populated by Slice 2.
    public const string SignedDocument = "signed_document";
    public const string DocusignEnvelope = "docusign_envelope";
    public const string AcroformSignature = "acroform_signature";
    public const string DocxSignatureLine = "docx_signature_line";
}

public static class ManifestBands
{
    public const string Strong = "strong";   // confidence >= 0.80
    public const string Likely = "likely";   // 0.60 <= confidence < 0.80
    public const string Possible = "possible"; // 0.35 <= confidence < 0.60
    public const string Skipped = "skipped";  // < 0.35

    public static string From(decimal confidence) => confidence switch
    {
        >= 0.80m => Strong,
        >= 0.60m => Likely,
        >= 0.35m => Possible,
        _ => Skipped
    };

    public static string RecommendedAction(decimal confidence) => From(confidence) switch
    {
        Strong or Likely => ManifestRecommendedActions.Select,
        Possible => ManifestRecommendedActions.Optional,
        _ => ManifestRecommendedActions.Skip
    };
}

public static class ManifestRecommendedActions
{
    public const string Select = "select";
    public const string Optional = "optional";
    public const string Skip = "skip";
}

/// <summary>Backward-compat alias for code that still uses ManifestBandNames.</summary>
public static class ManifestBandNames
{
    public const string Strong = ManifestBands.Strong;
    public const string Likely = ManifestBands.Likely;
    public const string Possible = ManifestBands.Possible;
    public const string Skipped = ManifestBands.Skipped;
}

/// <summary>Backward-compat alias for code that still uses ManifestRecommendedActionNames.</summary>
public static class ManifestRecommendedActionNames
{
    public const string Select = ManifestRecommendedActions.Select;
    public const string Optional = ManifestRecommendedActions.Optional;
    public const string Skip = ManifestRecommendedActions.Skip;
}

public static class SignatureProviderNames
{
    public const string Docusign = "docusign";
    public const string Adobe = "adobe";
    public const string Native = "native";
    public const string AcroForm = "acroform";
    public const string Docx = "docx_signature_line";
}
