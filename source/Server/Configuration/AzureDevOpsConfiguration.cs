using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Shared.Model;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    public class AzureDevOpsConfiguration : ExtensionConfigurationDocument
    {
        public AzureDevOpsConfiguration() : base("AzureDevOps", "Octopus Deploy", "1.0")
        {
            Id = AzureDevOpsConfigurationStore.SingletonId;
        }

        public string BaseUrl { get; set; }
        [Encrypted]
        public string PersonalAccessToken { get; set; }
    }
}