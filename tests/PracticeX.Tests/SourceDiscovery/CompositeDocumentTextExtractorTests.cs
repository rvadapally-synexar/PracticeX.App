using PracticeX.Discovery.TextExtraction;

namespace PracticeX.Tests.SourceDiscovery;

public class CompositeDocumentTextExtractorTests
{
    [Fact]
    public void EmptyChain_ReturnsEmpty()
    {
        var composite = new CompositeDocumentTextExtractor(Array.Empty<IDocumentTextExtractor>());
        var result = composite.Extract([1, 2, 3], "application/pdf", "x.pdf");

        Assert.Equal(string.Empty, result.FullText);
        Assert.Empty(result.Pages);
        Assert.Equal("none", result.ExtractorName);
    }

    [Fact]
    public void DispatchesToFirstMatching()
    {
        var pdfStub = new StubExtractor("pdf-stub", canExtract: (m, f) => f.EndsWith(".pdf"), text: "pdf-content");
        var docxStub = new StubExtractor("docx-stub", canExtract: (m, f) => f.EndsWith(".docx"), text: "docx-content");

        var composite = new CompositeDocumentTextExtractor(new IDocumentTextExtractor[] { pdfStub, docxStub });

        var result = composite.Extract([1, 2, 3], "application/pdf", "agreement.pdf");

        Assert.Equal("pdf-content", result.FullText);
        Assert.Equal("pdf-stub", result.ExtractorName);
        Assert.Equal(1, pdfStub.ExtractCalls);
        Assert.Equal(0, docxStub.ExtractCalls);
    }

    [Fact]
    public void NoMatchingExtractor_ReturnsEmpty()
    {
        var stub = new StubExtractor("docx-stub", canExtract: (m, f) => f.EndsWith(".docx"), text: "docx-content");
        var composite = new CompositeDocumentTextExtractor(new IDocumentTextExtractor[] { stub });

        var result = composite.Extract([1, 2, 3], "application/pdf", "agreement.pdf");

        Assert.Equal(string.Empty, result.FullText);
        Assert.Empty(result.Pages);
        Assert.Equal("none", result.ExtractorName);
        Assert.Equal(0, stub.ExtractCalls);
    }

    private sealed class StubExtractor : IDocumentTextExtractor
    {
        private readonly Func<string, string, bool> _canExtract;
        private readonly string _text;

        public StubExtractor(string name, Func<string, string, bool> canExtract, string text)
        {
            Name = name;
            _canExtract = canExtract;
            _text = text;
        }

        public string Name { get; }
        public int ExtractCalls { get; private set; }

        public bool CanExtract(string mimeType, string fileName) => _canExtract(mimeType, fileName);

        public TextExtractionResult Extract(byte[] content, string mimeType, string fileName, int? maxPages = null)
        {
            ExtractCalls++;
            return new TextExtractionResult
            {
                FullText = _text,
                Pages = [new ExtractedPage(1, _text)],
                ExtractorName = Name
            };
        }
    }
}
