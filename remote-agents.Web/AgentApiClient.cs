using System.Text.Json;

namespace remote_agents.Web;

public class AgentApiClient(HttpClient httpClient)
{
    public async IAsyncEnumerable<string> GetResponseAsync()
    {
        using var response = await httpClient.GetAsync("/copilot/response");
        response.EnsureSuccessStatusCode();
        using var responseStream = await response.Content.ReadAsStreamAsync();

        await foreach (var message in JsonSerializer.DeserializeAsyncEnumerable<string>(responseStream))
        {
            yield return message;
        }
    }
}