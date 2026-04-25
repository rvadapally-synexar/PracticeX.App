using System.Security.Cryptography;
using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Connectors;
using PracticeX.Domain.Sources;

namespace PracticeX.Infrastructure.SourceDiscovery.Connectors;

/// <summary>
/// Folder/upload connector. Accepts files passed in DiscoveryRequest.Inputs
/// (typically a multipart upload, optionally preserving relative path from a
/// folder/zip). Produces one DiscoveredItem per file with a sha256 over its
/// content, used both for dedupe and for storing the preserved original.
///
/// This is read-only with respect to the user's filesystem — the API does not
/// crawl the host machine; the browser uploads the bytes.
/// </summary>
public sealed class LocalFolderConnector : ISourceConnector
{
    public string SourceType => SourceTypes.LocalFolder;

    public ConnectorDescriptor Describe() => new(
        SourceType: SourceType,
        DisplayName: "Local folder upload",
        Summary: "Drag-and-drop a folder or files. Relative paths are preserved as folder hints; every upload is hashed and de-duplicated.",
        AuthMode: ConnectorAuthMode.None,
        SupportedMimeTypes: ["application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "image/tiff", "image/png", "image/jpeg", "text/plain"],
        IsReadOnly: true,
        Status: "ready"
    );

    public async Task<Result<DiscoveryResult>> DiscoverAsync(DiscoveryRequest request, CancellationToken cancellationToken)
    {
        if (request.Inputs is null || request.Inputs.Count == 0)
        {
            return Result<DiscoveryResult>.Fail("no_inputs", "Folder upload requires at least one input file.");
        }

        var items = new List<DiscoveredItem>(request.Inputs.Count);
        var notes = new List<DiscoveryNote>();

        foreach (var input in request.Inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var memory = new MemoryStream();
            await input.Content.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();
            var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

            var relativePath = NormalizeRelativePath(input.RelativePath);
            var folderHint = ExtractFolderHint(relativePath);

            var hints = new List<string>();
            if (!string.IsNullOrEmpty(folderHint))
            {
                hints.Add($"folder_hint:{folderHint}");
            }

            items.Add(new DiscoveredItem
            {
                ExternalId = $"{relativePath ?? input.Name}|{sha}",
                Name = Path.GetFileName(input.Name),
                MimeType = string.IsNullOrWhiteSpace(input.MimeType) ? "application/octet-stream" : input.MimeType,
                RelativePath = relativePath,
                ParentExternalId = string.IsNullOrEmpty(folderHint) ? null : folderHint,
                ObjectKind = SourceObjectKinds.File,
                SizeBytes = bytes.LongLength,
                Sha256 = sha,
                Sha256OfContent = sha,
                SourceCreatedAt = DateTimeOffset.UtcNow,
                SourceModifiedAt = DateTimeOffset.UtcNow,
                Uri = $"upload:{relativePath ?? input.Name}",
                InlineContent = bytes,
                Hints = hints
            });
        }

        return Result<DiscoveryResult>.Ok(new DiscoveryResult
        {
            Items = items,
            Notes = notes
        });
    }

    private static string? NormalizeRelativePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Replace('\\', '/').Trim('/');
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? ExtractFolderHint(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return null;
        }

        var lastSlash = relativePath.LastIndexOf('/');
        return lastSlash <= 0 ? null : relativePath[..lastSlash];
    }
}
