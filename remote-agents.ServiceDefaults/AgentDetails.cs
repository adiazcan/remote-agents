using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteAgents.ServiceDefaults;

/// <summary>
/// Describes the remote agent
/// </summary>
public class AgentDetails
{
    /// <summary>
    /// The agent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The agent instructions.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;
}