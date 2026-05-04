using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PracticeX.Discovery.TextExtraction;

/// <summary>
/// XLSX text extractor backed by DocumentFormat.OpenXml 3.2.0. Renders each
/// visible worksheet as a tab-separated table with the sheet name as a heading,
/// concatenated into FullText. One <see cref="ExtractedPage"/> per sheet so
/// downstream renderers and snippet panes can show structure.
///
/// Caps:
///   * Per-sheet rows: 5,000 (more than enough for headlines / due-diligence
///     metadata; refuses to dump six-figure rent rolls into the LLM context).
///   * Per-sheet columns: 50.
///   * Per-cell text: 500 chars.
///
/// Cell values resolve through the shared-strings table for inline strings;
/// formula cells emit their cached calculated value when present, falling back
/// to the formula text. Defensive: never throws — returns Empty + Notes.
/// </summary>
public sealed class XlsxTextExtractor : IDocumentTextExtractor
{
    private const int MaxRowsPerSheet = 5_000;
    private const int MaxColumnsPerSheet = 50;
    private const int MaxCellChars = 500;

    public string Name => "xlsx-text";

    public bool CanExtract(string mimeType, string fileName)
    {
        var mime = mimeType?.ToLowerInvariant() ?? string.Empty;
        if (mime.Contains("spreadsheetml.sheet") || mime.Contains("vnd.ms-excel"))
        {
            return true;
        }
        var name = fileName?.ToLowerInvariant() ?? string.Empty;
        return name.EndsWith(".xlsx") || name.EndsWith(".xlsm") || name.EndsWith(".xltx");
    }

    public TextExtractionResult Extract(byte[] content, string mimeType, string fileName, int? maxPages = null)
    {
        if (content is null || content.Length == 0)
        {
            return TextExtractionResult.Empty with { ExtractorName = Name, Notes = "empty" };
        }

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var doc = SpreadsheetDocument.Open(stream, isEditable: false);

            var workbookPart = doc.WorkbookPart;
            if (workbookPart?.Workbook?.Sheets is null)
            {
                return TextExtractionResult.Empty with { ExtractorName = Name, Notes = "no-workbook" };
            }

            var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable
                ?.Elements<SharedStringItem>()
                .Select(ssi => ssi.InnerText)
                .ToArray() ?? Array.Empty<string>();

            var sheets = workbookPart.Workbook.Sheets.Elements<Sheet>().ToList();
            var pages = new List<ExtractedPage>();
            var headings = new List<ExtractedHeading>();
            var fullTextParts = new List<string>();
            var sheetNumber = 0;

            foreach (var sheet in sheets)
            {
                if (sheet.Id?.Value is null) continue;
                var hidden = sheet.State?.Value is { } state &&
                             (state == SheetStateValues.Hidden || state == SheetStateValues.VeryHidden);
                if (hidden) continue;

                sheetNumber++;
                if (workbookPart.GetPartById(sheet.Id.Value) is not WorksheetPart wsp) continue;

                var sheetName = sheet.Name?.Value ?? $"Sheet{sheetNumber}";
                headings.Add(new ExtractedHeading(sheetName, PageNumber: sheetNumber, Level: 1));

                var sheetText = RenderSheet(wsp, sharedStrings, sheetName);
                pages.Add(new ExtractedPage(sheetNumber, sheetText));
                fullTextParts.Add(sheetText);
            }

            if (pages.Count == 0)
            {
                return TextExtractionResult.Empty with { ExtractorName = Name, Notes = "no-visible-sheets" };
            }

            var fullText = string.Join("\n\n", fullTextParts);

            return new TextExtractionResult
            {
                FullText = fullText,
                Pages = pages,
                Headings = headings,
                ExtractorName = Name,
                Truncated = false
            };
        }
        catch (Exception ex)
        {
            return TextExtractionResult.Empty with { ExtractorName = Name, Notes = ex.Message };
        }
    }

    private static string RenderSheet(WorksheetPart wsp, string[] sharedStrings, string sheetName)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("# ").AppendLine(sheetName);

        var rowCount = 0;
        using var reader = OpenXmlReader.Create(wsp);
        while (reader.Read())
        {
            if (reader.ElementType != typeof(Row)) continue;
            var row = (Row)reader.LoadCurrentElement()!;
            if (rowCount++ >= MaxRowsPerSheet)
            {
                sb.AppendLine("…(truncated)");
                break;
            }

            var cells = row.Elements<Cell>().Take(MaxColumnsPerSheet).ToList();
            if (cells.Count == 0) continue;

            // Render cells in their column order, padding gaps so the row
            // shape stays readable when the source file has sparse rows.
            var cellsByCol = new SortedDictionary<int, string>();
            foreach (var cell in cells)
            {
                var col = ColumnFromReference(cell.CellReference?.Value);
                if (col <= 0 || col > MaxColumnsPerSheet) continue;
                cellsByCol[col] = ResolveCellText(cell, sharedStrings);
            }

            if (cellsByCol.Count == 0) continue;

            var first = true;
            var maxCol = cellsByCol.Keys.Last();
            for (var c = 1; c <= maxCol; c++)
            {
                if (!first) sb.Append('\t');
                first = false;
                sb.Append(cellsByCol.TryGetValue(c, out var text) ? text : string.Empty);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string ResolveCellText(Cell cell, string[] sharedStrings)
    {
        var raw = cell.CellValue?.InnerText ?? cell.InnerText ?? string.Empty;
        var dataType = cell.DataType?.Value;

        string text;
        if (dataType == CellValues.SharedString)
        {
            if (int.TryParse(raw, out var idx) && idx >= 0 && idx < sharedStrings.Length)
            {
                text = sharedStrings[idx];
            }
            else
            {
                text = string.Empty;
            }
        }
        else if (dataType == CellValues.InlineString)
        {
            text = cell.InlineString?.Text?.Text ?? cell.InnerText ?? string.Empty;
        }
        else if (dataType == CellValues.Boolean)
        {
            text = raw == "1" ? "TRUE" : "FALSE";
        }
        else
        {
            text = raw;
        }

        text = (text ?? string.Empty).Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (text.Length > MaxCellChars) text = text[..MaxCellChars] + "…";
        return text;
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
}
