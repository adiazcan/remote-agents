using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteAgents.AppHost;

internal static class Constants
{
    internal static class Settings
    {
        internal static class AzureOpenAIOptions
        {
            internal const string ChatModelDeploymentName = @"AzureOpenAIOptions:ChatModelDeploymentName";

            internal const string ChatModelName = @"AzureOpenAIOptions:ChatModelName";

            internal const string EmbeddingsModelDeploymentName = @"AzureOpenAIOptions:EmbeddingsModelDeploymentName";

            internal const string EmbeddingsModelName = @"AzureOpenAIOptions:EmbeddingsModelName";

            internal const string Endpoint = @"AzureOpenAIOptions:Endpoint";

            internal const string Key = @"AzureOpenAIOptions:Key";

            internal const string UseTokenCredentialAuthentication = @"AzureOpenAIOptions:UseTokenCredentialAuthentication";

            internal const string TokenCredentialClientId = @"AzureOpenAIOptions:TokenCredentialsOptions:ClientId";

            internal const string TokenCredentialClientSecret = @"AzureOpenAIOptions:TokenCredentialsOptions:ClientSecret";

            internal const string TokenCredentialTenantId = $@"AzureOpenAIOptions:TokenCredentialsOptions:TenantId";

            internal const string UseDefaultAzureCredentialAuthentication = @"AzureOpenAIOptions:UseDefaultAzureCredentialAuthentication";
        }
    }
}
