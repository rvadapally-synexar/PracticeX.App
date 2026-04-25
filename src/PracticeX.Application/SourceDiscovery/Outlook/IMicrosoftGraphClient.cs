namespace PracticeX.Application.SourceDiscovery.Outlook;

public interface IMicrosoftGraphClient
{
    /// <summary>
    /// Search messages in the authenticated user's mailbox using a Graph $search
    /// or $filter query. Returns lightweight metadata only — bodies and attachments
    /// are fetched lazily via separate calls.
    /// </summary>
    Task<IReadOnlyList<GraphMailMessage>> SearchMessagesAsync(string accessToken, GraphMailQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// List attachments for a specific message. Returned attachments are metadata
    /// only; bytes are loaded via DownloadAttachmentAsync.
    /// </summary>
    Task<IReadOnlyList<GraphMailAttachment>> ListAttachmentsAsync(string accessToken, string messageId, CancellationToken cancellationToken);

    Task<Stream> DownloadAttachmentAsync(string accessToken, string messageId, string attachmentId, CancellationToken cancellationToken);
}

public sealed class GraphMailQuery
{
    public string? SearchExpression { get; init; }
    public DateTimeOffset? Since { get; init; }
    public int Top { get; init; } = 25;
    public bool HasAttachmentsOnly { get; init; } = true;
}

public sealed record GraphMailMessage(
    string Id,
    string Subject,
    string? FromAddress,
    string? FromName,
    DateTimeOffset ReceivedAt,
    bool HasAttachments,
    string? WebLink
);

public sealed record GraphMailAttachment(
    string Id,
    string MessageId,
    string Name,
    string ContentType,
    long Size
);
