using EuropeChat.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EuropeChat.Api.IntegrationTests.Infrastructure;

public class WebAppFactory : WebApplicationFactory<EuropeChatMarker>, IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = null!;

    public Task InitializeAsync()
    {
        HttpClient = CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
} 

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<WebAppFactory>;

