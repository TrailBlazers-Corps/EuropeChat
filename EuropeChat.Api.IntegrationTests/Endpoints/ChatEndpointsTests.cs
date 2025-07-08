using System.Net;
using System.Net.Http.Json;
using EuropeChat.Api.IntegrationTests.Infrastructure;
using EuropeChat.Models;
using FluentAssertions;
using Xunit;

namespace EuropeChat.Api.IntegrationTests.Endpoints;

[Collection("IntegrationTests")]
public class ChatEndpointsTests(WebAppFactory factory) :  IAsyncLifetime
{
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task Post_Chat_Returns_Ok()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new ChatRequest("Hello, answer only Hello");

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        chatResponse.Should().NotBeNull();
        chatResponse!.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_Conversations_Returns_Ok()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/conversations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversations = await response.Content.ReadFromJsonAsync<List<Conversation>>();
        conversations.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_Conversation_By_Id_Returns_Ok()
    {
        // Arrange
        var client = factory.CreateClient();
        var chatRequest = new ChatRequest("Hello, answer only Hello");
        var chatResponse = await client.PostAsJsonAsync("/api/chat", chatRequest);
        var conversation = await chatResponse.Content.ReadFromJsonAsync<ChatResponse>();

        // Act
        var response = await client.GetAsync($"/api/conversations/{conversation!.ConversationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Conversation>();
        result.Should().NotBeNull();
    }
} 