using Octopus.Data.Storage.Configuration;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    class AzureDevOpsConfigurationStore : ExtensionConfigurationStore<AzureDevOpsConfiguration>,
        IAzureDevOpsConfigurationStore
    {
        public static string CommentParser = "Azure DevOps";
        public static string SingletonId = "issuetracker-azuredevops-v2";

        public AzureDevOpsConfigurationStore(IConfigurationStore configurationStore) : base(configurationStore)
        {
        }

        public override string Id => SingletonId;

        public void SetConnections(AzureDevOpsConnection[] connections)
        {
            SetProperty(doc => doc.Connections = connections);
        }

        public AzureDevOpsConnection[] GetConnections()
        {
            return GetProperty(doc => doc.Connections);
        }
    }
}