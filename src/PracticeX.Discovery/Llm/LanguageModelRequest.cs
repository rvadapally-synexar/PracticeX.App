namespace PracticeX.Discovery.Llm;

public sealed record LanguageModelRequest
{
    /// <summary>Optional system prompt.</summary>
    public string? System { get; init; }

    /// <summary>Conversation messages, oldest first.</summary>
    public required IReadOnlyList<LanguageModelMessage> Messages { get; init; }

    /// <summary>Maximum tokens for the completion.</summary>
    public int MaxTokens { get; init; } = 1024;

    /// <summary>Temperature (0.0 deterministic, 1.0 creative). Default: 0.0 for extraction.</summary>
    public double Temperature { get; init; } = 0.0;

    /// <summary>
    /// Optional JSON schema for structured output. When set, providers should
    /// return JSON conforming to it (Anthropic tool-use, OpenAI structured outputs).
    /// </summary>
    public string? JsonSchema { get; init; }

    /// <summary>Tag for telemetry, e.g. "nda-extraction-v1".</summary>
    public string? Purpose { get; init; }
}

public sealed record LanguageModelMessage(string Role, string Content);

public static class LanguageModelRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string System = "system";
}
