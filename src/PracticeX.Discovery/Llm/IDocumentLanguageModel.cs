namespace PracticeX.Discovery.Llm;

/// <summary>
/// Per-provider LLM seam. Cloud binds AnthropicViaOpenRouterLanguageModel +
/// AzureOpenAiLanguageModel. The desktop agent's --mode local-prefilter binds
/// OllamaLanguageModel. Same interface, different transports, different
/// availability profiles. Failover orchestration lives in ILanguageModelRouter.
/// </summary>
public interface IDocumentLanguageModel
{
    /// <summary>Stable name used by the router for breaker bookkeeping and logs.</summary>
    string Name { get; }

    /// <summary>Returns false when API keys / endpoints are missing — router skips.</summary>
    bool IsConfigured { get; }

    Task<LanguageModelResponse> CompleteAsync(
        LanguageModelRequest request,
        CancellationToken cancellationToken);
}
