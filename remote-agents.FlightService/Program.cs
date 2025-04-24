using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
});

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

builder.Configuration.AddJsonFile($@"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($@"appsettings.{Environment.UserName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        builder.Configuration.GetValue<string>("AzureOpenAIOptions:ChatModelDeploymentName"),
        builder.Configuration.GetValue<string>("AzureOpenAIOptions:Endpoint"),
        builder.Configuration.GetValue<string>("AzureOpenAIOptions:Key")
    );

builder.Services.AddSingleton<ChatCompletionAgent>(builder =>
{
    return new()
    {
        Name = "FlightExpert",
        Instructions =
            """
                Your are an expert in flight travel and you are specialized in organizing flight trips by identifying the best flight options for your clients.
                Your goal is to create a flight plan to reach a city based on the user preferences and budget.
                You don't have experience on any other travel options, so you can only suggest flight options.
                You're laser focused on the goal at hand. 
                You can provide plans only about flights. Do not include plans around lodging, meals or sightseeing.
                Once you have generated a flight plan, don't ask the user for feedback or further suggestions. Stick with it.
                Don't waste time with chit chat. 
                Don't say goodbye and don't wish the user a good trip.
            """,
        Kernel = builder.GetRequiredService<Kernel>()
    };
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/agent/details", (ChatCompletionAgent agent) =>
{
    var details = new
    {
        Name = agent.Name,
        Instructions = agent.Instructions
    };
    return JsonSerializer.Serialize(details);
});

app.MapPost("/agent/invoke", async (ChatCompletionAgent agent, HttpResponse response, ChatHistory history) =>
{
    response.Headers.Append("Content-Type", "application/json");

    var thread = new ChatHistoryAgentThread();

    await foreach (var chatResponse in agent.InvokeAsync(history, thread).ConfigureAwait(false))
    {
        chatResponse.Message.AuthorName = agent.Name;

        return JsonSerializer.Serialize(chatResponse.Message);
    }

    return null;
});

app.MapPost("/agent/invoke-streaming", async (ChatCompletionAgent agent, HttpResponse response, ChatHistory history) =>
{
    response.Headers.Append("Content-Type", "application/jsonl");

    var thread = new ChatHistoryAgentThread();

    var chatResponse = agent.InvokeStreamingAsync(history, thread).ConfigureAwait(false);
    await foreach (var delta in chatResponse)
    {
        var message = new StreamingChatMessageContent(AuthorRole.Assistant, delta.Message.Content)
        {
            AuthorName = agent.Name
        };

        await response.WriteAsync(JsonSerializer.Serialize(message)).ConfigureAwait(false);
        await response.Body.FlushAsync().ConfigureAwait(false);
    }
});

app.Run();
