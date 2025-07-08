using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.OpenApi.Models;
using EuropeChat.Data;
using EuropeChat.Models;
using EuropeChat.Services;
using System.Reflection;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EuropeChat.Api", Version = "v1" });

    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});

// Add Database
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



// Register AI providers
builder.Services.AddSingleton<IAIProvider, ChatGPTProvider>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable Swagger UI based on configuration
var enableSwagger = app.Configuration.GetValue<bool>("EnableSwagger");

if (enableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EuropeChat.Api v1"));
}

// Ensure database is created only when not in development
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    dbContext.Database.EnsureCreated();
}

// app.MapObservability();

// Add Serilog request logging
app.UseSerilogRequestLogging();

// app.MapPrometheusScrapingEndpoint(); // Ensure this is commented out or removed

// app.UseHttpsRedirection();
app.UseCors();

// Chat endpoints
app.MapPost("/api/chat", async (ChatRequest request, ChatService chatService) =>
{
    try
    {
        var response = await chatService.SendMessageAsync(request);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An unexpected error occurred while processing the chat request");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
})
.WithName("CreateChat")
.WithOpenApi();

app.MapGet("/api/conversations", async (ChatService chatService) =>
{
    var conversations = await chatService.GetAllConversationsAsync();
    return Results.Ok(conversations);
})
.WithName("GetAllConversations")
.WithOpenApi();

app.MapGet("/api/conversations/{id:guid}", async (Guid id, ChatService chatService) =>
{
    var conversation = await chatService.GetConversationAsync(id);
    if (conversation == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(conversation);
})
.WithName("GetConversationById")
.WithOpenApi();

app.Run();

// Make Program class accessible for integration tests
public interface EuropeChatMarker { }