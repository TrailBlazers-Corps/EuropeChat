namespace EuropeChat.Models;

/// <summary>
/// Represents a conversation between a user and an AI assistant.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the title or subject of the conversation.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the timestamp when the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the list of messages within this conversation.
    /// </summary>
    public List<Message> Messages { get; set; } = new();
}

/// <summary>
/// Represents a single message within a conversation.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the ID of the conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the role of the sender (e.g., User, Assistant).
    /// </summary>
    public MessageRole Role { get; set; }
    /// <summary>
    /// Gets or sets the AI provider used for the message (e.g., ChatGPT).
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the timestamp when the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the role of the sender of a message.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// The message was sent by the user.
    /// </summary>
    User,
    /// <summary>
    /// The message was sent by the AI assistant.
    /// </summary>
    Assistant
}

/// <summary>
/// Represents a request to send a message to the chat service.
/// </summary>
/// <param name="Message">The content of the message to send.</param>
/// <param name="ConversationId">The optional ID of an existing conversation. If null, a new conversation will be created.</param>
/// <param name="Provider">The AI provider to use for the chat. Defaults to "ChatGPT".</param>
public record ChatRequest(string Message, string? ConversationId = null, string Provider = "ChatGPT");

/// <summary>
/// Represents the response from the chat service.
/// </summary>
/// <param name="Response">The AI's response message.</param>
/// <param name="ConversationId">The ID of the conversation.</param>
public record ChatResponse(string Response, string ConversationId);

