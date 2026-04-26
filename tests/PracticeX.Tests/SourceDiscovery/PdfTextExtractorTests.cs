using System.Text;
using PracticeX.Discovery.TextExtraction;

namespace PracticeX.Tests.SourceDiscovery;

public class PdfTextExtractorTests
{
    private readonly PdfTextExtractor _extractor = new();

    [Fact]
    public void CanExtract_OnlyPdf()
    {
        Assert.True(_extractor.CanExtract("application/pdf", "x.pdf"));
        Assert.True(_extractor.CanExtract("application/octet-stream", "x.pdf"));
        Assert.False(_extractor.CanExtract("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "x.docx"));
        Assert.False(_extractor.CanExtract("text/plain", "x.txt"));
    }

    [Fact]
    public void Extract_BogusBytes_ReturnsEmpty()
    {
        var result = _extractor.Extract(Encoding.UTF8.GetBytes("not a pdf"), "application/pdf", "fake.pdf");

        Assert.Equal(string.Empty, result.FullText);
        Assert.Empty(result.Pages);
        Assert.Equal("pdf-text", result.ExtractorName);
        Assert.False(string.IsNullOrEmpty(result.Notes));
    }

    [Fact]
    public void Extract_EmptyContent_ReturnsEmpty()
    {
        var result = _extractor.Extract([], "application/pdf", "empty.pdf");

        Assert.Equal(string.Empty, result.FullText);
        Assert.Empty(result.Pages);
        Assert.Equal("pdf-text", result.ExtractorName);
        Assert.Equal("empty", result.Notes);
    }
}
