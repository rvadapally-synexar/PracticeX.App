using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PracticeX.Application.SourceDiscovery.Outlook;

namespace PracticeX.Infrastructure.SourceDiscovery.Outlook;

/// <summary>
/// Read-only HTTP client over Microsoft Graph v1.0. Uses $search/$filter to find
/// messages with attachments that are likely to contain contracts. The caller
/// supplies the access token; refreshing is handled outside this class.
/// </summary>
public sealed class MicrosoftGraphClient(IHttpClientFactory httpClientFactory) : IMicrosoftGraphClient
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";

    public async Task<IReadOnlyList<GraphMailMessage>> SearchMessagesAsync(string accessToken, GraphMailQuery query, CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient("microsoft-graph");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var top = Math.Clamp(query.Top, 1, 100);
        var url = $"{GraphBase}/me/messages?$top={top}&$select=id,subject,from,receivedDateTime,hasAttachments,webLink";

        if (!string.IsNullOrWhiteSpace(query.SearchExpression))
        {
            url += $"&$search=\"{Uri.EscapeDataString(query.SearchExpression)}\"";
        }

        var filterClauses = new List<string>();
        if (query.HasAttachmentsOnly)
        {
            filterClauses.Add("hasAttachments eq true");
        }
        if (query.Since.HasValue)
        {
            filterClauses.Add($"receivedDateTime ge {query.Since.Value.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}");
        }
        if (filterClauses.Count > 0 && string.IsNullOrWhiteSpace(query.SearchExpression))
        {
            url += "&$filter=" + Uri.EscapeDataString(string.Join(" and ", filterClauses));
        }

        var response = await http.GetFromJsonAsync<MessagesResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Microsoft Graph messages endpoint.");

        return response.Value
            .Select(m => new GraphMailMessage(
                Id: m.Id,
                Subject: m.Subject ?? string.Empty,
                FromAddress: m.From?.EmailAddress?.Address,
                FromName: m.From?.EmailAddress?.Name,
                ReceivedAt: m.ReceivedDateTime,
                HasAttachments: m.HasAttachments,
                WebLink: m.WebLink))
            .ToList();
    }

    public async Task<IReadOnlyList<GraphMailAttachment>> ListAttachmentsAsync(string accessToken, string messageId, CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient("microsoft-graph");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var url = $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}/attachments?$select=id,name,contentType,size";
        var response = await http.GetFromJsonAsync<AttachmentsResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Microsoft Graph attachments endpoint.");

        return response.Value
            .Select(a => new GraphMailAttachment(
                Id: a.Id,
                MessageId: messageId,
                Name: a.Name ?? "attachment",
                ContentType: a.ContentType ?? "application/octet-stream",
                Size: a.Size))
            .ToList();
    }

    public async Task<Stream> DownloadAttachmentAsync(string accessToken, string messageId, string attachmentId, CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient("microsoft-graph");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var url = $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}/attachments/{Uri.EscapeDataString(attachmentId)}/$value";
        var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private sealed record MessagesResponse(
        [property: JsonPropertyName("value")] List<MessagePayload> Value
    );

    private sealed record MessagePayload(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("subject")] string? Subject,
        [property: JsonPropertyName("from")] FromPayload? From,
        [property: JsonPropertyName("receivedDateTime")] DateTimeOffset ReceivedDateTime,
        [property: JsonPropertyName("hasAttachments")] bool HasAttachments,
        [property: JsonPropertyName("webLink")] string? WebLink
    );

    private sealed record FromPayload(
        [property: JsonPropertyName("emailAddress")] EmailAddressPayload? EmailAddress
    );

    private sealed record EmailAddressPayload(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("address")] string? Address
    );

    private sealed record AttachmentsResponse(
        [property: JsonPropertyName("value")] List<AttachmentPayload> Value
    );

    private sealed record AttachmentPayload(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("contentType")] string? ContentType,
        [property: JsonPropertyName("size")] long Size
    );
}
