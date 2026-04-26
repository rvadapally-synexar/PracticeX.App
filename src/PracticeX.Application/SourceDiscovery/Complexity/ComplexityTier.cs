namespace PracticeX.Application.SourceDiscovery.Complexity;

/// <summary>
/// Per-file processing-effort tier. Used for routing (which Doc AI provider,
/// which extraction strategy) and pricing (hours, dollars).
///
/// Validity errors gate ingestion; complexity tier never does — Tier X files
/// are still persisted, just flagged for human review and routed away from
/// auto-extraction.
/// </summary>
public enum ComplexityTier
{
    Simple = 0,    // 'S' — flat, deterministic, near-zero processing cost
    Moderate = 1,  // 'M' — multi-section but mechanical
    Large = 2,     // 'L' — needs custom mapping or expensive OCR
    Extra = 3      // 'X' — manual review required (macros, encrypted, oversize)
}

public static class ComplexityTierExtensions
{
    public static string ToCode(this ComplexityTier tier) => tier switch
    {
        ComplexityTier.Simple => "S",
        ComplexityTier.Moderate => "M",
        ComplexityTier.Large => "L",
        ComplexityTier.Extra => "X",
        _ => "S"
    };

    public static ComplexityTier FromCode(string code) => code switch
    {
        "S" => ComplexityTier.Simple,
        "M" => ComplexityTier.Moderate,
        "L" => ComplexityTier.Large,
        "X" => ComplexityTier.Extra,
        _ => ComplexityTier.Simple
    };
}

public static class ComplexityFactors
{
    // Universal
    public const string OversizeFile         = "oversize_file";

    // Excel-specific
    public const string MultiSheet           = "multi_sheet";
    public const string ManySheets           = "many_sheets";
    public const string HasFormulas          = "has_formulas";
    public const string ManyFormulas         = "many_formulas";
    public const string HeaderMergedCells    = "header_merged_cells";
    public const string DataMergedCells      = "data_merged_cells";
    public const string PivotTables          = "pivot_tables";
    public const string NamedRanges          = "named_ranges";
    public const string CrossSheetRefs       = "cross_sheet_refs";
    public const string ConditionalFormat    = "conditional_format_heavy";
    public const string HiddenSheets         = "hidden_sheets";
    public const string ExternalLinks        = "external_workbook_links";

    // PDF-specific
    public const string ManyPages            = "many_pages";
    public const string LargePdf             = "large_pdf";
    public const string FormFields           = "form_fields";
    public const string EmbeddedFiles        = "embedded_files";
    public const string PdfJavaScript        = "pdf_javascript";
    public const string ScannedPdf           = "scanned_pdf";

    // DOCX-specific
    public const string TrackedChanges       = "tracked_changes";
    public const string Comments             = "comments";
    public const string SignatureLines       = "signature_lines";
    public const string EmbeddedObjects      = "embedded_objects";
    public const string LongDocument         = "long_document";
}

public static class ComplexityBlockers
{
    public const string MacrosDetected       = "macros_detected";
    public const string PasswordProtected    = "password_protected";
    public const string OversizeFile         = "oversize_file";
    public const string ExternalLinks        = "external_workbook_links";
    public const string OleObjects           = "ole_objects";
}
