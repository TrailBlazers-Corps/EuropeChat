using EuropeChat.Models;
using EuropeChat.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EuropeChat.Services;

public class ChatService
{
    private readonly Dictionary<string, IAIProvider> _providers;
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<ChatService> _logger;
    private static readonly ActivitySource ActivitySource = new("EuropeChat.ChatService");

    public ChatService(IEnumerable<IAIProvider> providers, ChatDbContext dbContext, ILogger<ChatService> logger)
    {
        _providers = providers.ToDictionary(p => p.Name, p => p);
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        using var activity = ActivitySource.StartActivity("SendMessage");
        activity?.SetTag("provider", request.Provider);
        
        _logger.LogInformation("Processing chat request with provider {Provider}", request.Provider);

        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();
        
        var conversation = await _dbContext.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(conversationId));

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.Parse(conversationId),
                Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message
            };
            _dbContext.Conversations.Add(conversation);
        }

        // Add user message
        var userMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Message,
            Role = MessageRole.User,
            Provider = request.Provider
        };
        conversation.Messages.Add(userMessage);

        // Get AI response
        if (!_providers.TryGetValue(request.Provider, out var provider))
        {
            _logger.LogError("Provider {Provider} not found", request.Provider);
            throw new ArgumentException($"Provider '{request.Provider}' not found");
        }

        var response = await provider.GenerateResponseAsync(request.Message, conversation.Messages.ToList());

        // Add assistant message
        var assistantMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = response,
            Role = MessageRole.Assistant,
            Provider = request.Provider
        };
        conversation.Messages.Add(assistantMessage);
        
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Chat response generated successfully for conversation {ConversationId}", conversationId);
        
        return new ChatResponse(response, conversationId);
    }

    public async Task<Conversation?> GetConversationAsync(Guid conversationId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Messages.OrderBy(m => m.Timestamp))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    public async Task<IEnumerable<Conversation>> GetAllConversationsAsync()
    {
        return await _dbContext.Conversations
            .Include(c => c.Messages)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
