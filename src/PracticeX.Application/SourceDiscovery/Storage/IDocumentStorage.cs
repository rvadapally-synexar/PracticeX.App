namespace PracticeX.Application.SourceDiscovery.Storage;

/// <summary>
/// Abstraction over preserved-original document storage. Local filesystem in dev,
/// Azure Blob (or equivalent) in production. Storage URIs are opaque to callers.
/// </summary>
public interface IDocumentStorage
{
    Task<StoredDocument> StoreAsync(Guid tenantId, string suggestedName, Stream content, string mimeType, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageUri, CancellationToken cancellationToken);
}

public sealed record StoredDocument(string StorageUri, string Sha256, long SizeBytes);
