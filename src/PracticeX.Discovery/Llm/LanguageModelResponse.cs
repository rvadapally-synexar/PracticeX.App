namespace PracticeX.Discovery.Llm;

public sealed record LanguageModelResponse(
    string Text,
    int TokensIn,
    int TokensOut,
    string ProviderName,
    string Model,
    long LatencyMs,
    string? StopReason = null
);
