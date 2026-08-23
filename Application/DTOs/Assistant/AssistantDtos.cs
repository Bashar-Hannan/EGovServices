namespace EGovServices.Application.DTOs.Assistant;

/// <summary>
/// The frontend sends the full conversation history with every request.
/// The backend is stateless — it does not store any chat history.
/// </summary>
public sealed record ChatRequest
{
    public required List<ChatMessage> Messages { get; init; } = [];
}

public sealed record ChatMessage
{
    public required string Role { get; init; }      // "user" or "assistant"
    public required string Content { get; init; }
}

//public sealed record ChatResponse
//{
//    public required string Reply { get; init; }
//}
public sealed record ChatResponse(
    string Reply      // رد المساعد
);