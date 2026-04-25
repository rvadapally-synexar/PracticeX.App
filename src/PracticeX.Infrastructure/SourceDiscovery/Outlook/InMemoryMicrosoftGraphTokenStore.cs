using System.Collections.Concurrent;
using PracticeX.Application.SourceDiscovery.Outlook;

namespace PracticeX.Infrastructure.SourceDiscovery.Outlook;

/// <summary>
/// Process-local token store for development. NOT FOR PRODUCTION — production
/// must persist refresh tokens in Azure Key Vault (or equivalent) keyed by
/// source connection id, with envelope encryption.
/// </summary>
public sealed class InMemoryMicrosoftGraphTokenStore : IMicrosoftGraphTokenStore
{
    private readonly ConcurrentDictionary<Guid, StoredGraphToken> _store = new();

    public Task SaveAsync(Guid sourceConnectionId, StoredGraphToken token, CancellationToken cancellationToken)
    {
        _store[sourceConnectionId] = token;
        return Task.CompletedTask;
    }

    public Task<StoredGraphToken?> GetAsync(Guid sourceConnectionId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(sourceConnectionId, out var stored);
        return Task.FromResult<StoredGraphToken?>(stored);
    }

    public Task DeleteAsync(Guid sourceConnectionId, CancellationToken cancellationToken)
    {
        _store.TryRemove(sourceConnectionId, out _);
        return Task.CompletedTask;
    }
}
