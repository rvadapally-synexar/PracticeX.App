using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Discovery.Validation;
using PracticeX.Domain.Documents;

namespace PracticeX.Infrastructure.SourceDiscovery.Complexity;

/// <summary>
/// Tiering rules for Excel (per plan A4):
///   S: 1 visible sheet AND ≤20 cols AND ≤10k rows AND no formulas AND no merged data cells
///      AND no macros AND no external links
///   M: 2-5 sheets OR formulas 1-99 OR header-only merged cells
///   L: 6+ sheets OR formulas ≥100 OR data-row merged cells OR pivot tables OR cross-sheet refs
///      OR named ranges OR conditional formatting rules ≥5
///   X: macros (.xlsm) OR external workbook links OR password-protected OR OLE objects
///      OR file > 100 MB OR > 50 sheets
/// </summary>
public sealed class ExcelComplexityProfiler
{
    private const long OversizeBytes = 100L * 1024 * 1024;

    public ComplexityReport Profile(byte[] content, string fileName, ValidityReport validity)
    {
        var factors = new List<string>();
        var blockers = new List<string>();
        var sizeBytes = content.LongLength;

        if (sizeBytes > OversizeBytes)
        {
            factors.Add(ComplexityFactors.OversizeFile);
            blockers.Add(ComplexityBlockers.OversizeFile);
        }

        // .xlsm = macro-enabled. Identifying via extension is the cheap path;
        // we also scan the package parts below to catch renamed files.
        var macroByExtension = fileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase);
        if (macroByExtension)
        {
            factors.Add(ComplexityFactors.HasFormulas); // sentinel — overwritten if more specific signals fire
            blockers.Add(ComplexityBlockers.MacrosDetected);
        }

        if (validity.IsEncrypted == true || validity.ValidityStatus == ValidityStatuses.Encrypted)
        {
            blockers.Add(ComplexityBlockers.PasswordProtected);
            return Report(ComplexityTier.Extra, factors, blockers, BuildMetadata(format: "xlsx", sizeBytes, sheetCount: 0, hiddenSheetCount: 0,
                visibleSheetCount: 0, maxRows: 0, maxColumns: 0, formulaCount: 0, mergedRegionCount: 0, pivotTableCount: 0,
                namedRangeCount: 0, externalLinkCount: 0, hasMacros: macroByExtension, conditionalFormatCount: 0));
        }

        int sheetCount = 0, hiddenSheets = 0, visibleSheets = 0, maxRows = 0, maxCols = 0;
        int formulaCount = 0, mergedRegionCount = 0, pivotCount = 0, namedRangeCount = 0;
        int externalLinkCount = 0, conditionalFormatCount = 0;
        var hasDataMergedCells = false;
        var hasMacros = macroByExtension;

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var doc = SpreadsheetDocument.Open(stream, isEditable: false);

            // Macro detection via package part (catches .xlsx renamed from .xlsm)
            if (doc.WorkbookPart?.VbaProjectPart is not null)
            {
                hasMacros = true;
                if (!blockers.Contains(ComplexityBlockers.MacrosDetected))
                {
                    blockers.Add(ComplexityBlockers.MacrosDetected);
                }
            }

            // External workbook links
            if (doc.WorkbookPart?.ExternalWorkbookParts.Any() == true)
            {
                externalLinkCount = doc.WorkbookPart.ExternalWorkbookParts.Count();
                factors.Add(ComplexityFactors.ExternalLinks);
                blockers.Add(ComplexityBlockers.ExternalLinks);
            }

            // Named ranges (workbook-level + sheet-level)
            namedRangeCount = doc.WorkbookPart?.Workbook?.DefinedNames?.ChildElements.Count ?? 0;
            if (namedRangeCount > 0) factors.Add(ComplexityFactors.NamedRanges);

            // Sheet enumeration
            var sheets = doc.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().ToList() ?? new List<Sheet>();
            sheetCount = sheets.Count;
            hiddenSheets = sheets.Count(s =>
                s.State?.Value is { } state &&
                (state == SheetStateValues.Hidden || state == SheetStateValues.VeryHidden));
            visibleSheets = sheetCount - hiddenSheets;
            if (hiddenSheets > 0) factors.Add(ComplexityFactors.HiddenSheets);

            foreach (var sheet in sheets)
            {
                if (sheet.Id?.Value is null) continue;
                if (doc.WorkbookPart!.GetPartById(sheet.Id.Value) is not WorksheetPart wsp) continue;

                var ws = wsp.Worksheet;

                // Pivot tables
                pivotCount += wsp.PivotTableParts.Count();

                // Conditional formatting rules
                var cfBlocks = ws.Descendants<ConditionalFormatting>().ToList();
                conditionalFormatCount += cfBlocks.Sum(cf => cf.Elements<ConditionalFormattingRule>().Count());

                // Merged cells: distinguish header-only (row 1) from data-row merges
                var merges = ws.Descendants<MergeCell>().ToList();
                mergedRegionCount += merges.Count;
                if (!hasDataMergedCells)
                {
                    foreach (var mc in merges)
                    {
                        var refStr = mc.Reference?.Value;
                        if (string.IsNullOrEmpty(refStr)) continue;
                        // A merge is "data-row" if any row referenced > 1.
                        if (TryParseMaxRow(refStr, out var maxRow) && maxRow > 1)
                        {
                            hasDataMergedCells = true;
                            break;
                        }
                    }
                }

                // Row / column / formula counts via streaming reader (cheap pass)
                var sheetMaxRow = 0;
                var sheetMaxCol = 0;
                using var reader = OpenXmlReader.Create(wsp);
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Row))
                    {
                        var row = (Row)reader.LoadCurrentElement()!;
                        if (row.RowIndex?.Value is uint ri && ri > sheetMaxRow) sheetMaxRow = (int)ri;
                        foreach (var cell in row.Elements<Cell>())
                        {
                            if (cell.CellFormula is not null) formulaCount++;
                            var col = ColumnFromReference(cell.CellReference?.Value);
                            if (col > sheetMaxCol) sheetMaxCol = col;
                        }
                    }
                }
                if (sheetMaxRow > maxRows) maxRows = sheetMaxRow;
                if (sheetMaxCol > maxCols) maxCols = sheetMaxCol;
            }
        }
        catch
        {
            // Validity inspector already gates corruption; if OpenXml chokes
            // on a "valid" zip we treat it as Moderate so a human reviews.
            return Report(ComplexityTier.Moderate, factors, blockers,
                BuildMetadata("xlsx", sizeBytes, sheetCount, hiddenSheets, visibleSheets, maxRows, maxCols,
                    formulaCount, mergedRegionCount, pivotCount, namedRangeCount, externalLinkCount, hasMacros, conditionalFormatCount));
        }

        if (sheetCount >= 2) factors.Add(ComplexityFactors.MultiSheet);
        if (sheetCount >= 6) factors.Add(ComplexityFactors.ManySheets);
        if (formulaCount > 0) factors.Add(ComplexityFactors.HasFormulas);
        if (formulaCount >= 100) factors.Add(ComplexityFactors.ManyFormulas);
        if (pivotCount > 0) factors.Add(ComplexityFactors.PivotTables);
        if (conditionalFormatCount >= 5) factors.Add(ComplexityFactors.ConditionalFormat);
        if (mergedRegionCount > 0)
        {
            factors.Add(hasDataMergedCells ? ComplexityFactors.DataMergedCells : ComplexityFactors.HeaderMergedCells);
        }

        var tier = AssignTier(sheetCount, visibleSheets, maxRows, maxCols, formulaCount, mergedRegionCount,
            hasDataMergedCells, pivotCount, namedRangeCount, externalLinkCount, hasMacros, conditionalFormatCount,
            sizeBytes);

        return Report(tier, factors.Distinct().ToList(), blockers.Distinct().ToList(),
            BuildMetadata("xlsx", sizeBytes, sheetCount, hiddenSheets, visibleSheets, maxRows, maxCols,
                formulaCount, mergedRegionCount, pivotCount, namedRangeCount, externalLinkCount, hasMacros, conditionalFormatCount));
    }

    private static ComplexityTier AssignTier(
        int sheetCount, int visibleSheets, int maxRows, int maxCols,
        int formulaCount, int mergedRegionCount, bool hasDataMergedCells,
        int pivotCount, int namedRangeCount, int externalLinkCount,
        bool hasMacros, int conditionalFormatCount, long sizeBytes)
    {
        if (hasMacros || externalLinkCount > 0 || sizeBytes > OversizeBytes || sheetCount > 50)
            return ComplexityTier.Extra;

        if (sheetCount >= 6 || formulaCount >= 100 || hasDataMergedCells || pivotCount > 0
            || namedRangeCount > 0 || conditionalFormatCount >= 5)
            return ComplexityTier.Large;

        if (sheetCount >= 2 || formulaCount > 0 || mergedRegionCount > 0)
            return ComplexityTier.Moderate;

        if (visibleSheets <= 1 && maxCols <= 20 && maxRows <= 10_000)
            return ComplexityTier.Simple;

        return ComplexityTier.Moderate;
    }

    private static bool TryParseMaxRow(string mergeReference, out int maxRow)
    {
        // mergeReference is like "A1:B3" or "C5:F12" — we want the larger row number.
        maxRow = 0;
        var parts = mergeReference.Split(':');
        if (parts.Length != 2) return false;
        return TryRow(parts[0], out var a) && TryRow(parts[1], out var b)
            && (maxRow = Math.Max(a, b)) >= 0;

        static bool TryRow(string r, out int row)
        {
            row = 0;
            var digits = new string(r.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out row);
        }
    }

    private static int ColumnFromReference(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        var col = 0;
        foreach (var ch in cellRef)
        {
            if (ch is >= 'A' and <= 'Z') col = col * 26 + (ch - 'A' + 1);
            else if (ch is >= 'a' and <= 'z') col = col * 26 + (ch - 'a' + 1);
            else break;
        }
        return col;
    }

    private static ComplexityReport Report(ComplexityTier tier, List<string> factors, List<string> blockers, string metadataJson)
        => new()
        {
            Tier = tier,
            Factors = factors,
            Blockers = blockers,
            MetadataJson = metadataJson
        };

    private static string BuildMetadata(
        string format, long sizeBytes, int sheetCount, int hiddenSheetCount, int visibleSheetCount,
        int maxRows, int maxColumns, int formulaCount, int mergedRegionCount, int pivotTableCount,
        int namedRangeCount, int externalLinkCount, bool hasMacros, int conditionalFormatCount)
        => JsonSerializer.Serialize(new
        {
            format,
            sizeBytes,
            sheetCount,
            visibleSheetCount,
            hiddenSheetCount,
            maxRows,
            maxColumns,
            formulaCount,
            mergedRegionCount,
            pivotTableCount,
            namedRangeCount,
            externalLinkCount,
            hasMacros,
            conditionalFormatCount
        });
}
