using System.Text.Json;
using Microsoft.Extensions.Logging;
using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Connectors;
using PracticeX.Application.SourceDiscovery.Outlook;
using PracticeX.Domain.Documents;
using PracticeX.Domain.Sources;

namespace PracticeX.Infrastructure.SourceDiscovery.Connectors;

/// <summary>
/// Outlook mailbox connector. Uses Microsoft Graph $search to find messages with
/// attachments that match contract-likely keywords, lists attachments, and emits
/// each attachment as a DiscoveredItem with the message metadata captured as a
/// parent source object. Read-only.
/// </summary>
public sealed class OutlookGraphConnector(
    IMicrosoftGraphClient graphClient,
    IMicrosoftGraphOAuthService oauthService,
    IMicrosoftGraphTokenStore tokenStore,
    ILogger<OutlookGraphConnector> logger) : ISourceConnector
{
    private static readonly string[] ContractSearchKeywords =
    [
        "contract", "agreement", "amendment", "renewal", "lease", "fee schedule"
    ];

    public string SourceType => SourceTypes.OutlookMailbox;

    public ConnectorDescriptor Describe() => new(
        SourceType: SourceType,
        DisplayName: "Outlook mailbox",
        Summary: "Read-only Microsoft Graph search across the connected mailbox for messages with contract-like attachments.",
        AuthMode: ConnectorAuthMode.OAuth,
        SupportedMimeTypes: ["application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword"],
        IsReadOnly: true,
        Status: oauthService.IsConfigured ? "ready" : "configuration_required"
    );

    public async Task<Result<DiscoveryResult>> DiscoverAsync(DiscoveryRequest request, CancellationToken cancellationToken)
    {
        var stored = await tokenStore.GetAsync(request.ConnectionId, cancellationToken);
        if (stored is null)
        {
            return Result<DiscoveryResult>.Fail("not_authorized", "Outlook connection has no stored credentials. Complete the OAuth flow first.");
        }

        string accessToken;
        try
        {
            if (stored.CachedAccessToken is not null && stored.CachedAccessTokenExpiresAt is { } expiry && expiry > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                accessToken = stored.CachedAccessToken;
            }
            else
            {
                var lease = await oauthService.RefreshAccessTokenAsync(stored.RefreshToken, cancellationToken);
                accessToken = lease.AccessToken;
                await tokenStore.SaveAsync(request.ConnectionId, stored with
                {
                    CachedAccessToken = lease.AccessToken,
                    CachedAccessTokenExpiresAt = lease.ExpiresAt
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to refresh Microsoft Graph access token for connection {ConnectionId}", request.ConnectionId);
            return Result<DiscoveryResult>.Fail("refresh_failed", ex.Message);
        }

        var items = new List<DiscoveredItem>();
        var notes = new List<DiscoveryNote>();
        var top = Math.Min(request.MaxItems ?? 25, 100);

        var searchExpression = string.Join(" OR ", ContractSearchKeywords.Select(k => $"\\\"{k}\\\""));

        IReadOnlyList<GraphMailMessage> messages;
        try
        {
            messages = await graphClient.SearchMessagesAsync(accessToken, new GraphMailQuery
            {
                SearchExpression = searchExpression,
                Since = request.Since,
                HasAttachmentsOnly = true,
                Top = top
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<DiscoveryResult>.Fail("graph_search_failed", ex.Message);
        }

        foreach (var msg in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!msg.HasAttachments)
            {
                continue;
            }

            // Parent record (the message itself).
            var hints = new List<string>();
            if (LooksContractRelated(msg.Subject))
            {
                hints.Add(IngestionReasonCodes.OutlookSubjectKeywords);
            }
            if (HasContractDomain(msg.FromAddress))
            {
                hints.Add(IngestionReasonCodes.OutlookSenderDomain);
            }

            items.Add(new DiscoveredItem
            {
                ExternalId = $"msg:{msg.Id}",
                Name = string.IsNullOrWhiteSpace(msg.Subject) ? "(no subject)" : msg.Subject,
                MimeType = "message/rfc822",
                ObjectKind = SourceObjectKinds.MailMessage,
                SourceCreatedAt = msg.ReceivedAt,
                SourceModifiedAt = msg.ReceivedAt,
                Uri = msg.WebLink ?? $"graph://messages/{msg.Id}",
                Hints = hints,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    from = msg.FromAddress,
                    fromName = msg.FromName,
                    receivedAt = msg.ReceivedAt,
                    webLink = msg.WebLink
                })
            });

            IReadOnlyList<GraphMailAttachment> attachments;
            try
            {
                attachments = await graphClient.ListAttachmentsAsync(accessToken, msg.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                notes.Add(new DiscoveryNote
                {
                    Code = "attachment_list_failed",
                    Severity = "warn",
                    Subject = msg.Id,
                    Message = ex.Message
                });
                continue;
            }

            foreach (var att in attachments)
            {
                if (!IsLikelyContractAttachment(att))
                {
                    continue;
                }

                byte[]? bytes = null;
                try
                {
                    using var stream = await graphClient.DownloadAttachmentAsync(accessToken, msg.Id, att.Id, cancellationToken);
                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory, cancellationToken);
                    bytes = memory.ToArray();
                }
                catch (Exception ex)
                {
                    notes.Add(new DiscoveryNote
                    {
                        Code = "attachment_download_failed",
                        Severity = "warn",
                        Subject = att.Id,
                        Message = ex.Message
                    });
                    continue;
                }

                items.Add(new DiscoveredItem
                {
                    ExternalId = $"msg:{msg.Id}|att:{att.Id}",
                    ParentExternalId = $"msg:{msg.Id}",
                    Name = att.Name,
                    MimeType = att.ContentType,
                    ObjectKind = SourceObjectKinds.MailAttachment,
                    SizeBytes = att.Size,
                    SourceCreatedAt = msg.ReceivedAt,
                    SourceModifiedAt = msg.ReceivedAt,
                    Uri = $"graph://messages/{msg.Id}/attachments/{att.Id}",
                    InlineContent = bytes,
                    Hints = hints
                });
            }
        }

        return Result<DiscoveryResult>.Ok(new DiscoveryResult
        {
            Items = items,
            Notes = notes
        });
    }

    private static bool LooksContractRelated(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return false;
        }
        var lowered = subject.ToLowerInvariant();
        return ContractSearchKeywords.Any(k => lowered.Contains(k));
    }

    private static bool HasContractDomain(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            return false;
        }
        var lowered = sender.ToLowerInvariant();
        return lowered.Contains("legal") || lowered.Contains("counsel") || lowered.Contains("contracts");
    }

    private static bool IsLikelyContractAttachment(GraphMailAttachment attachment)
    {
        if (attachment.Size > 25 * 1024 * 1024)
        {
            return false;
        }

        var ext = Path.GetExtension(attachment.Name).ToLowerInvariant();
        return ext is ".pdf" or ".doc" or ".docx" or ".tif" or ".tiff";
    }
}
