using System.Text.Json.Serialization;

namespace vissb;
public record Choice(
        [property: JsonPropertyName("message")] Message Message,
        [property: JsonPropertyName("finish_reason")] string FinishReason,
        [property: JsonPropertyName("index")] int Index
    );

public record Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public record OpenAiResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("object")] string Object,
    [property: JsonPropertyName("created")] int Created,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("usage")] Usage Usage,
    [property: JsonPropertyName("choices")] IReadOnlyList<Choice> Choices
);

public record Usage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens
);


