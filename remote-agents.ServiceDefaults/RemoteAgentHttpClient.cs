using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RemoteAgents.ServiceDefaults;

/// <summary>
/// Http client for remote agent.
/// </summary>
/// <param name="httpClient">An inner client</param>
public class RemoteAgentHttpClient(HttpClient httpClient)
{
    /// <summary>
    /// Get agent details.
    /// </summary>
    public Task<HttpResponseMessage> GetAgentDetailsAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2234 // We cannot pass uri here since we are using a customer http client with a base address
        return httpClient.GetAsync("/agent/details", cancellationToken);
    }

    /// <summary>
    /// Invoke the agent with the provided history.
    /// </summary>
    public Task<HttpResponseMessage> InvokeAsync(ChatHistory history, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2234 // We cannot pass uri here since we are using a customer http client with a base address
        return httpClient.PostAsync("/agent/invoke", new StringContent(JsonSerializer.Serialize(history), Encoding.UTF8, "application/json"), cancellationToken);
    }

    /// <summary>
    /// Invoke the agent with the provided history and stream the response.
    /// </summary>
    public Task<HttpResponseMessage> InvokeStreamingAsync(ChatHistory history, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2234 // We cannot pass uri here since we are using a customer http client with a base address
        return httpClient.PostAsync("/agent/invoke-streaming", new StringContent(JsonSerializer.Serialize(history), Encoding.UTF8, "application/json"), cancellationToken);
    }
}