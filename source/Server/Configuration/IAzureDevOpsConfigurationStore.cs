using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    public interface IAzureDevOpsConfigurationStore : IExtensionConfigurationStore<AzureDevOpsConfiguration>
    {
        string GetBaseUrl();
        void SetBaseUrl(string baseUrl);
    }
}