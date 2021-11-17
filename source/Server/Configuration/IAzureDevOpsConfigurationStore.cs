using System.Collections.Generic;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    interface IAzureDevOpsConfigurationStore : IExtensionConfigurationStore<AzureDevOpsConfiguration>
    {
        List<AzureDevOpsConnection> GetConnections();
    }
}