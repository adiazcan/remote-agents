using RemoteAgents.ServiceDefaults;

namespace RemoteAgents.ApiService;

internal sealed class FlightAgentHttpClient : RemoteAgentHttpClient
{
    public FlightAgentHttpClient(HttpClient httpClient) : base(httpClient)
    {
    }
}
