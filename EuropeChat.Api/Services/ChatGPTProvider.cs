using Azure;
using Azure.AI.OpenAI;
using EuropeChat.Models;
using OpenAI.Chat;
using System.Diagnostics;

namespace EuropeChat.Services;

public class ChatGPTProvider : IAIProvider
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<ChatGPTProvider> _logger;
    private static readonly ActivitySource ActivitySource = new("EuropeChat.ChatGPTProvider");

    public string Name => "ChatGPT";

    public ChatGPTProvider(IConfiguration configuration, ILogger<ChatGPTProvider> logger)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";
        _logger = logger;

        _client = new AzureOpenAIClient(
            new Uri(endpoint!),
            new AzureKeyCredential(apiKey!)
        );
    }

    public async Task<string> GenerateResponseAsync(string message, List<Message> conversationHistory)
    {
        using var activity = ActivitySource.StartActivity("GenerateResponse");
        activity?.SetTag("deployment", _deploymentName);
        activity?.SetTag("message_length", message.Length);
        activity?.SetTag("history_count", conversationHistory.Count);

        _logger.LogInformation("Generating ChatGPT response for message with {HistoryCount} previous messages", conversationHistory.Count);

        var chatClient = _client.GetChatClient(_deploymentName);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful AI assistant.")
        };

        // Add conversation history
        foreach (var historyMessage in conversationHistory.TakeLast(10)) // Limit to last 10 messages
        {
            messages.Add(historyMessage.Role == MessageRole.User
                ? new UserChatMessage(historyMessage.Content)
                : new AssistantChatMessage(historyMessage.Content));
        }

        // Add current user message
        messages.Add(new UserChatMessage(message));

        try
        {
            var response = await chatClient.CompleteChatAsync(messages);
            var responseText = response.Value.Content[0].Text;
            
            _logger.LogInformation("ChatGPT response generated successfully, length: {ResponseLength}", responseText.Length);
            activity?.SetTag("response_length", responseText.Length);
            
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ChatGPT response");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
