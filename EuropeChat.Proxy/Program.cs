using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var greeterMeter = new Meter("EuropeChat.Proxy", "1.0.0");
var countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");
var requestDuration = greeterMeter.CreateHistogram<double>("request_duration", unit: "ms", description: "Duration of requests");

var greeterActivitySource = new ActivitySource("EuropeChat.Proxy");

var otel = builder.Services.AddOpenTelemetry();

otel.ConfigureResource(resource => resource
    .AddService(serviceName: "EuropeChat.Proxy"));

otel.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddMeter(greeterMeter.Name)
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
    .AddMeter("System.Net.Http")
    .AddMeter("System.Net.NameResolution")
    .AddPrometheusExporter());

var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5185";
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Use(async (HttpContext context, RequestDelegate next) =>
{
    if (context.Request.Path.StartsWithSegments("/health") || 
        context.Request.Path.StartsWithSegments("/metrics"))
    {
        await next(context);
        return;
    }
    
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    
    using var activity = greeterActivitySource.StartActivity($"Proxy {context.Request.Method} {context.Request.Path}");
    var sw = Stopwatch.StartNew();
    
    try
    {
        countGreetings.Add(1, new KeyValuePair<string, object?>("method", context.Request.Method),
                                new KeyValuePair<string, object?>("path", context.Request.Path.Value));
        
        var httpClient = httpClientFactory.CreateClient("ApiClient");
        
        var targetUrl = $"{context.Request.Path}{context.Request.QueryString}";
        
        var requestMessage = new HttpRequestMessage();
        requestMessage.RequestUri = new Uri(httpClient.BaseAddress!, targetUrl);
        requestMessage.Method = new HttpMethod(context.Request.Method);
        
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.ToLower() != "host" && 
                header.Key.ToLower() != "connection" &&
                header.Key.ToLower() != "transfer-encoding")
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }
        
        if (context.Request.Method != "GET" && context.Request.Method != "HEAD" && context.Request.ContentLength > 0)
        {
            var content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType != null)
            {
                content.Headers.Add("Content-Type", context.Request.ContentType);
            }
            requestMessage.Content = content;
        }
        
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.url", targetUrl);
        activity?.SetTag("proxy.target", httpClient.BaseAddress?.ToString());
        
        logger.LogInformation("Proxying {Method} request to {Path}", context.Request.Method, targetUrl);
        
        var response = await httpClient.SendAsync(requestMessage);
        
        context.Response.StatusCode = (int)response.StatusCode;
        
        foreach (var header in response.Headers)
        {
            if (header.Key.ToLower() != "transfer-encoding")
            {
                context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
            }
        }
        
        foreach (var header in response.Content.Headers)
        {
            if (header.Key.ToLower() != "transfer-encoding")
            {
                context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
            }
        }
        
        await response.Content.CopyToAsync(context.Response.Body);
        
        sw.Stop();
        
        requestDuration.Record(sw.ElapsedMilliseconds, 
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("path", context.Request.Path.Value),
            new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
        
        activity?.SetTag("http.status_code", context.Response.StatusCode);
        activity?.SetTag("duration_ms", sw.ElapsedMilliseconds);
        
        logger.LogInformation("Completed {Method} request to {Path} with status {StatusCode} in {Duration}ms", 
            context.Request.Method, targetUrl, context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        sw.Stop();
        
        requestDuration.Record(sw.ElapsedMilliseconds, 
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("path", context.Request.Path.Value),
            new KeyValuePair<string, object?>("status_code", 500));
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("error", true);
        activity?.SetTag("exception.message", ex.Message);
        activity?.SetTag("exception.type", ex.GetType().FullName);
        
        logger.LogError(ex, "Error proxying {Method} request to {Path}: {ErrorMessage}", 
            context.Request.Method, context.Request.Path, ex.Message);
        
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Proxy Error: " + ex.Message);
    }
});

app.MapPrometheusScrapingEndpoint();

app.MapGet("/health", () =>
{
    using var activity = greeterActivitySource.StartActivity("Health Check");
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Health check requested");
    activity?.SetTag("health.status", "healthy");
    
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow, service = "EuropeChat.Proxy" });
})
.WithName("GetHealth");

app.Run();

public partial class Program { }
