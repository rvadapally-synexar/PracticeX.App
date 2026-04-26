namespace PracticeX.Discovery.Llm;

/// <summary>
/// Orchestrates a chain of IDocumentLanguageModel providers with failover. The
/// chain is registration-order: first registered = primary, then fallbacks.
/// Skips providers where IsConfigured is false; opens a circuit breaker per
/// provider after consecutive failures.
/// </summary>
public interface ILanguageModelRouter
{
    Task<LanguageModelResponse> CompleteAsync(
        LanguageModelRequest request,
        CancellationToken cancellationToken);
}

public sealed class LanguageModelRouterOptions
{
    public const string SectionName = "LanguageModels:FailoverPolicy";

    public int TimeoutSeconds { get; set; } = 60;
    public int BreakerThreshold { get; set; } = 3;
    public int BreakerCooldownSeconds { get; set; } = 300;
}
