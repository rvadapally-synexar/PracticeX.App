using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PracticeX.Discovery.TextExtraction;

namespace PracticeX.Tests.SourceDiscovery;

public class DocxTextExtractorTests
{
    private readonly DocxTextExtractor _extractor = new();

    [Fact]
    public void CanExtract_OnlyDocx()
    {
        Assert.True(_extractor.CanExtract("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "x.docx"));
        Assert.True(_extractor.CanExtract("application/octet-stream", "letter.docx"));
        Assert.False(_extractor.CanExtract("application/pdf", "x.pdf"));
        Assert.False(_extractor.CanExtract("text/plain", "x.txt"));
    }

    [Fact]
    public void Extract_RealDocx_ReturnsParagraphText()
    {
        var docx = BuildDocx(new (string Style, string Text)[]
        {
            ("Heading1", "Master Services Agreement"),
            (null!,      "This is the first paragraph."),
            (null!,      "This is the second paragraph.")
        });

        var result = _extractor.Extract(docx, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "letter.docx");

        Assert.Equal("docx-text", result.ExtractorName);
        Assert.Contains("Master Services Agreement", result.FullText);
        Assert.Contains("first paragraph", result.FullText);
        Assert.Contains("second paragraph", result.FullText);
        Assert.Single(result.Pages);
        Assert.Equal(1, result.Pages[0].PageNumber);
        Assert.False(result.Truncated);

        var heading = Assert.Single(result.Headings);
        Assert.Equal("Master Services Agreement", heading.Text);
        Assert.Equal(1, heading.Level);
        Assert.Equal(1, heading.PageNumber);
    }

    [Fact]
    public void Extract_NotZip_ReturnsEmpty()
    {
        var result = _extractor.Extract(Encoding.UTF8.GetBytes("definitely not a zip file"), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "x.docx");

        Assert.Equal(string.Empty, result.FullText);
        Assert.Empty(result.Pages);
        Assert.Equal("docx-text", result.ExtractorName);
        Assert.False(string.IsNullOrEmpty(result.Notes));
    }

    private static byte[] BuildDocx(IEnumerable<(string Style, string Text)> paragraphs)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            foreach (var (style, text) in paragraphs)
            {
                var p = new Paragraph();
                if (!string.IsNullOrEmpty(style))
                {
                    p.AppendChild(new ParagraphProperties(
                        new ParagraphStyleId { Val = style }));
                }
                p.AppendChild(new Run(new Text(text)));
                body.AppendChild(p);
            }
        }
        return ms.ToArray();
    }
}
