using System.Text.Json.Serialization;

namespace PracticeX.Discovery.Contracts;

public sealed record ProblemDetailsDto(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("detail")] string? Detail
);
