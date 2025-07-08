using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Custom metrics for the application
var greeterMeter = new Meter("EuropeChat.Proxy", "1.0.0");
var countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");
var requestDuration = greeterMeter.CreateHistogram<double>("request_duration", unit: "ms", description: "Duration of requests");

// Custom ActivitySource for the application
var greeterActivitySource = new ActivitySource("EuropeChat.Proxy");

// Get configuration
var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"] ?? "http://localhost:4317";

// Configure OpenTelemetry
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: "EuropeChat.Proxy"));

// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
otel.WithMetrics(metrics => metrics
    // Metrics provider from OpenTelemetry
    .AddAspNetCoreInstrumentation()
    .AddMeter(greeterMeter.Name)
    // Metrics provides by ASP.NET Core in .NET 8
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
    // Metrics provided by System.Net libraries
    .AddMeter("System.Net.Http")
    .AddMeter("System.Net.NameResolution")
    .AddPrometheusExporter()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(tracingOtlpEndpoint);
    }));

// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource(greeterActivitySource.Name);
    if (!string.IsNullOrEmpty(tracingOtlpEndpoint))
    {
        tracing.AddOtlpExporter(otlpOptions =>
         {
             otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
         });
    }
    else
    {
        tracing.AddConsoleExporter();
    }
});

// Configure Logging with OpenTelemetry
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(x =>
{
    x.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("EuropeChat.Proxy", serviceVersion: "1.0.0"));
    x.IncludeFormattedMessage = true;
    x.IncludeScopes = true;
    x.ParseStateValues = true;
    x.AddConsoleExporter();
});

// Add HTTP client for proxying requests
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5185";
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Create activity source for custom tracing (using the one defined above)
// The meter and activitySource are already defined above

// Configure proxy middleware to forward all requests to API
app.Use(async (HttpContext context, RequestDelegate next) =>
{
    // Skip proxy for health check and metrics endpoints
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
        // Increment request counter
        countGreetings.Add(1, new KeyValuePair<string, object?>("method", context.Request.Method),
                                new KeyValuePair<string, object?>("path", context.Request.Path.Value));
        
        // Create HTTP client
        var httpClient = httpClientFactory.CreateClient("ApiClient");
        
        // Build the target URL
        var targetUrl = $"{context.Request.Path}{context.Request.QueryString}";
        
        // Create the request message
        var requestMessage = new HttpRequestMessage();
        requestMessage.RequestUri = new Uri(httpClient.BaseAddress!, targetUrl);
        requestMessage.Method = new HttpMethod(context.Request.Method);
        
        // Copy headers (excluding host and some others)
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
        
        // Copy body for non-GET requests
        if (context.Request.Method != "GET" && context.Request.Method != "HEAD" && context.Request.ContentLength > 0)
        {
            var content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType != null)
            {
                content.Headers.Add("Content-Type", context.Request.ContentType);
            }
            requestMessage.Content = content;
        }
        
        // Add trace information to activity
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.url", targetUrl);
        activity?.SetTag("proxy.target", httpClient.BaseAddress?.ToString());
        
        logger.LogInformation("Proxying {Method} request to {Path}", context.Request.Method, targetUrl);
        
        // Send the request
        var response = await httpClient.SendAsync(requestMessage);
        
        // Copy response status
        context.Response.StatusCode = (int)response.StatusCode;
        
        // Copy response headers (excluding transfer-encoding to avoid double chunking)
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
        
        // Copy response body
        await response.Content.CopyToAsync(context.Response.Body);
        
        sw.Stop();
        
        // Record metrics
        requestDuration.Record(sw.ElapsedMilliseconds, 
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("path", context.Request.Path.Value),
            new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
        
        // Add trace information
        activity?.SetTag("http.status_code", context.Response.StatusCode);
        activity?.SetTag("duration_ms", sw.ElapsedMilliseconds);
        
        logger.LogInformation("Completed {Method} request to {Path} with status {StatusCode} in {Duration}ms", 
            context.Request.Method, targetUrl, context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        sw.Stop();
        
        // Record error metrics
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

// Configure the Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint();

// Health check endpoint
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

// Make the Program class accessible for testing
public partial class Program { }
