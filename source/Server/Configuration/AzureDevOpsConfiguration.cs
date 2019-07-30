using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

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

        public ReleaseNoteOptions ReleaseNoteOptions { get; set; } = new ReleaseNoteOptions();
    }

    public class ReleaseNoteOptions
    {
        public string ReleaseNotePrefix { get; set; }
    }
}