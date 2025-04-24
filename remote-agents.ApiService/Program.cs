using Encamina.Enmarcha.AI.OpenAI.Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using RemoteAgents.ApiService;
using RemoteAgents.ServiceDefaults;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
});

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

builder.Configuration.AddJsonFile($@"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($@"appsettings.{Environment.UserName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

builder.Services
    .AddOptionsWithValidateOnStart<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection(nameof(AzureOpenAIOptions)))
    .ValidateDataAnnotations();

builder.AddServiceDefaults();

builder.Services.AddHttpClient<FlightAgentHttpClient>(client => { client.BaseAddress = new("https+http://flightservice"); });

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        builder.Configuration.GetValue<string>("AzureOpenAIOptions:ChatModelDeploymentName"),
        builder.Configuration.GetValue<string>("AzureOpenAIOptions:Endpoint"),
        builder.Configuration.GetValue<string>("AzureOpenAIOptions:Key")
    );

var app = builder.Build();

ChatCompletionAgent GetAgent(Kernel kernel, string name, string instructions)
{
    ChatCompletionAgent agent =
        new()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel
        };

    return agent;
}

ChatCompletionAgent GetTravelAgent(Kernel kernel, string name)
{
    string Instructions =
        """
            You are a travel agent and you help users who wants to make a trip to visit a city. 
            The goal is to create a plan to visit a city based on the user preferences and budget.
            You don't have expertise on travel plans, so you can only suggest hotels, restaurants and places to see. You can't suggest traveling options like flights or trains.
            You're laser focused on the goal at hand. 
            Once you have generated a plan, don't ask the user for feedback or further suggestions. Stick with it.
            Don't waste time with chit chat. Don't say goodbye and don't wish the user a good trip.
        """;

    return GetAgent(kernel, name, Instructions);
}

ChatCompletionAgent GetManagerAgent(Kernel kernel, string name)
{
    string Instructions =
        """
            You are a travel manager and your goal is to validate a given trip plan. 
            You must make sure that the plan includes all the necessary details: transportation, lodging, meals and sightseeing. 
            If one of these details is missing, the plan is not good.
            If the plan is good, recap the entire plan into a Markdown table and say "the plan is approved".
            If not, write a paragraph to explain why it's not good and then provide an improved plan.
        """;

    return GetAgent(kernel, name, Instructions);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/response", async (Kernel kernel) =>
{
    var prompt = "What is the capital of Tenerife?";
    var result = await kernel.InvokePromptAsync(prompt);

    return Results.Ok(result.GetValue<string>());
})
.WithName("Response");

app.MapGet("/copilot/response", (Kernel kernel, FlightAgentHttpClient flightAgentHttpClient) =>
{
    return Results.Ok(GetCopilotResponse(kernel, flightAgentHttpClient));
}).Produces<IAsyncEnumerable<string>>();

async IAsyncEnumerable<string> GetCopilotResponse(Kernel kernel, FlightAgentHttpClient flightAgentHttpClient)
{
    string travelManagerName = "TravelManager";
    string travelAgentName = "TravelAgent";


    var flightAgent = new RemoteChatCompletionAgent(flightAgentHttpClient);
    var flightExpertName = flightAgent.Name;

    var managerAgent = GetManagerAgent(kernel, travelManagerName);
    var travelAgent = GetTravelAgent(kernel, travelAgentName);

    KernelFunction terminateFunction = KernelFunctionFactory.CreateFromPrompt(
        $$$"""
            Determine if the travel plan has been approved. If so, respond with a single word: yes.

            History:

            {{$history}}    
        """
        );

    KernelFunction selectionFunction = KernelFunctionFactory.CreateFromPrompt(
        $$$"""
              Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
              State only the name of the participant to take the next turn.

              Choose only from these participants:
              - {{{travelManagerName}}}
              - {{{travelAgentName}}}
              - {{{flightExpertName}}}

              Always follow these steps when selecting the next participant:
              1) After user input, it is {{{travelAgentName}}}'s turn.
              2) After {{{travelAgentName}}} replies, it's {{{flightExpertName}}}'s turn.
              3) After {{{flightExpertName}}} replies, it's {{{travelManagerName}}}'s turn to review and approve the plan.
              4) If the plan is approved, the conversation ends.
              5) If the plan isn't approved, it's {{{travelAgent}}}'s turn again.

              History:
              {{$history}}
          """
    );

    ChatHistoryTruncationReducer historyReducer = new(1);

    AgentGroupChat chat = new(managerAgent, travelAgent, flightAgent)
    {
        ExecutionSettings = new()
        {
            TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, kernel)
            {
                Agents = [managerAgent],
                ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                HistoryVariableName = "history",
                MaximumIterations = 10
            },
            SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
            {
                AgentsVariableName = "agents",
                HistoryVariableName = "history"
            }
        }
    };

    string prompt = "I live in Como, Italy and I would like to visit Paris. I'm on a budget, I want to travel by plane and I would like to stay for maximum 3 days. Please craft a trip plan for me";

    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, prompt));

    await foreach (var content in chat.InvokeAsync().ConfigureAwait(false))
    {
        var message = $"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'";

        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine();


        yield return message;
    }

    Console.WriteLine($"# IS COMPLETE: {chat.IsComplete}");
}




string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
