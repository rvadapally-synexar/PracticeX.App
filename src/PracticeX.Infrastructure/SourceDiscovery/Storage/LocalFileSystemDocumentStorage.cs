using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PracticeX.Application.SourceDiscovery.Storage;

namespace PracticeX.Infrastructure.SourceDiscovery.Storage;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>Root directory (or container path) where preserved originals are written.</summary>
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "practicex", "documents");
}

/// <summary>
/// Local file-system implementation. Files are content-addressed by sha256 within
/// a per-tenant directory so repeated uploads of identical content collapse onto
/// a single physical file.
/// </summary>
public sealed class LocalFileSystemDocumentStorage(IOptions<DocumentStorageOptions> options) : IDocumentStorage
{
    private readonly DocumentStorageOptions _options = options.Value;

    public async Task<StoredDocument> StoreAsync(Guid tenantId, string suggestedName, Stream content, string mimeType, CancellationToken cancellationToken)
    {
        // Resolve to an absolute path so Uri construction below works whether the
        // configured RootPath is relative ("./var/...") or absolute.
        var rootAbsolute = Path.GetFullPath(_options.RootPath);
        var tenantRoot = Path.Combine(rootAbsolute, tenantId.ToString("N"));
        Directory.CreateDirectory(tenantRoot);

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var fileName = $"{sha}{GetExtensionFromMime(mimeType, suggestedName)}";
        var fullPath = Path.Combine(tenantRoot, fileName);

        if (!File.Exists(fullPath))
        {
            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
        }

        var uri = new Uri(fullPath).AbsoluteUri;
        return new StoredDocument(uri, sha, bytes.LongLength);
    }

    public Task<Stream> OpenReadAsync(string storageUri, CancellationToken cancellationToken)
    {
        var path = new Uri(storageUri).LocalPath;
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    private static string GetExtensionFromMime(string mimeType, string suggestedName)
    {
        var ext = Path.GetExtension(suggestedName);
        if (!string.IsNullOrEmpty(ext))
        {
            return ext;
        }

        return mimeType.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "image/tiff" => ".tiff",
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "text/plain" => ".txt",
            _ => ".bin"
        };
    }
}
