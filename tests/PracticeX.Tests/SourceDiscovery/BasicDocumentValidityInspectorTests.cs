using System.IO.Compression;
using System.Text;
using PracticeX.Application.SourceDiscovery.Validation;
using PracticeX.Domain.Documents;
using PracticeX.Infrastructure.SourceDiscovery.Validation;

namespace PracticeX.Tests.SourceDiscovery;

public class BasicDocumentValidityInspectorTests
{
    private readonly BasicDocumentValidityInspector _inspector = new();

    [Fact]
    public void Inspect_EmptyFile_ReturnsSkipUnsupported()
    {
        var report = _inspector.Inspect([], "application/pdf", "empty.pdf");

        Assert.Equal(ValidityStatuses.Unsupported, report.ValidityStatus);
        Assert.Equal(ExtractionRoutes.Skip, report.ExtractionRoute);
        Assert.Contains("empty_file", report.ReasonCodes);
    }

    [Fact]
    public void Inspect_PlainText_RoutesToLocalText()
    {
        var bytes = Encoding.UTF8.GetBytes("This is a fee schedule for 2026.\nLine two.\n");
        var report = _inspector.Inspect(bytes, "text/plain", "fee-schedule.txt");

        Assert.Equal(ValidityStatuses.Valid, report.ValidityStatus);
        Assert.Equal(ExtractionRoutes.LocalText, report.ExtractionRoute);
        Assert.True(report.HasTextLayer);
        Assert.False(report.IsEncrypted);
    }

    [Fact]
    public void Inspect_BogusPdfBytes_FlagsCorrupt()
    {
        var bytes = Encoding.UTF8.GetBytes("not a real pdf — just bytes that happen to be here");
        var report = _inspector.Inspect(bytes, "application/pdf", "fake.pdf");

        Assert.Equal(ValidityStatuses.Corrupt, report.ValidityStatus);
        Assert.Equal(ExtractionRoutes.Skip, report.ExtractionRoute);
        Assert.Contains("corrupt_pdf", report.ReasonCodes);
    }

    [Fact]
    public void Inspect_SyntheticDocxZip_FlagsValid()
    {
        var bytes = BuildMinimalDocxZip();
        var report = _inspector.Inspect(
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "letter.docx");

        Assert.Equal(ValidityStatuses.Valid, report.ValidityStatus);
        Assert.Equal(ExtractionRoutes.LocalText, report.ExtractionRoute);
        Assert.True(report.HasTextLayer);
    }

    [Fact]
    public void Inspect_UnknownExtension_RoutesToSkip()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var report = _inspector.Inspect(bytes, "application/octet-stream", "mystery.bin");

        Assert.Equal(ValidityStatuses.Unsupported, report.ValidityStatus);
        Assert.Equal(ExtractionRoutes.Skip, report.ExtractionRoute);
        Assert.Contains("unsupported_container", report.ReasonCodes);
    }

    private static byte[] BuildMinimalDocxZip()
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("[Content_Types].xml");
            using var sw = new StreamWriter(entry.Open());
            sw.Write("<?xml version=\"1.0\"?><Types/>");
        }
        return ms.ToArray();
    }
}
