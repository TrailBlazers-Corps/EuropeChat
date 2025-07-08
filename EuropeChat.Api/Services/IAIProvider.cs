using EuropeChat.Models;

namespace EuropeChat.Services;

public interface IAIProvider
{
    string Name { get; }
    Task<string> GenerateResponseAsync(string message, List<Message> conversationHistory);
}

