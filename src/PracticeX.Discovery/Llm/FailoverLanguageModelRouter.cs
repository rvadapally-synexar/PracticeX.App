using System.Collections.Concurrent;

namespace PracticeX.Discovery.Llm;

/// <summary>
/// Default ILanguageModelRouter. Tries the chain in registration order; skips
/// providers that report IsConfigured=false; opens a circuit breaker per
/// provider after BreakerThreshold consecutive failures. The breaker auto-
/// resets after BreakerCooldownSeconds. No Polly dependency — kept tiny so
/// the agent can ship it without extra binaries.
/// </summary>
public sealed class FailoverLanguageModelRouter(
    IReadOnlyList<IDocumentLanguageModel> chain,
    LanguageModelRouterOptions options) : ILanguageModelRouter
{
    private readonly IReadOnlyList<IDocumentLanguageModel> _chain = chain;
    private readonly LanguageModelRouterOptions _options = options;
    private readonly ConcurrentDictionary<string, BreakerState> _breakers = new();

    public async Task<LanguageModelResponse> CompleteAsync(
        LanguageModelRequest request,
        CancellationToken cancellationToken)
    {
        if (_chain.Count == 0)
        {
            throw new InvalidOperationException(
                "No IDocumentLanguageModel providers registered. Add at least one provider before calling the router.");
        }

        var failures = new List<(string provider, Exception error)>();

        foreach (var provider in _chain)
        {
            if (!provider.IsConfigured)
            {
                failures.Add((provider.Name, new InvalidOperationException("provider_not_configured")));
                continue;
            }

            if (IsBreakerOpen(provider.Name))
            {
                failures.Add((provider.Name, new InvalidOperationException("breaker_open")));
                continue;
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                var response = await provider.CompleteAsync(request, cts.Token).ConfigureAwait(false);
                ResetBreaker(provider.Name);
                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // caller-cancelled; do not failover
            }
            catch (Exception ex)
            {
                RecordFailure(provider.Name);
                failures.Add((provider.Name, ex));
            }
        }

        var summary = string.Join("; ", failures.Select(f => $"{f.provider}: {f.error.GetType().Name} {f.error.Message}"));
        throw new LanguageModelRouterException(
            $"All {_chain.Count} language-model provider(s) failed. Details: {summary}",
            failures);
    }

    private bool IsBreakerOpen(string providerName)
    {
        if (!_breakers.TryGetValue(providerName, out var state))
        {
            return false;
        }
        if (state.ConsecutiveFailures < _options.BreakerThreshold)
        {
            return false;
        }
        var openSince = state.OpenedAtUtc;
        if (openSince is null)
        {
            return false;
        }
        var elapsed = DateTimeOffset.UtcNow - openSince.Value;
        return elapsed.TotalSeconds < _options.BreakerCooldownSeconds;
    }

    private void RecordFailure(string providerName)
    {
        _breakers.AddOrUpdate(
            providerName,
            _ => new BreakerState(1, DateTimeOffset.UtcNow),
            (_, prev) =>
            {
                var failures = prev.ConsecutiveFailures + 1;
                var openedAt = failures >= _options.BreakerThreshold && prev.OpenedAtUtc is null
                    ? (DateTimeOffset?)DateTimeOffset.UtcNow
                    : prev.OpenedAtUtc;
                return new BreakerState(failures, openedAt);
            });
    }

    private void ResetBreaker(string providerName) =>
        _breakers[providerName] = new BreakerState(0, null);

    private sealed record BreakerState(int ConsecutiveFailures, DateTimeOffset? OpenedAtUtc);
}

public sealed class LanguageModelRouterException(string message, IReadOnlyList<(string Provider, Exception Error)> failures)
    : Exception(message)
{
    public IReadOnlyList<(string Provider, Exception Error)> Failures { get; } = failures;
}
