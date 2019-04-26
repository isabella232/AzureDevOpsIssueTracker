using Octopus.Data.Storage.Configuration;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    public class AzureDevOpsConfigurationStore : ExtensionConfigurationStore<AzureDevOpsConfiguration>,
        IAzureDevOpsConfigurationStore
    {
        public static string CommentParser = "Azure DevOps";
        public static string SingletonId = "issuetracker-azuredevops";

        public AzureDevOpsConfigurationStore(IConfigurationStore configurationStore) : base(configurationStore)
        {
        }

        public override string Id => SingletonId;

        public string GetBaseUrl()
        {
            return GetProperty(doc => doc.BaseUrl?.Trim('/'));
        }

        public void SetBaseUrl(string baseUrl)
        {
            SetProperty(doc => doc.BaseUrl = baseUrl?.Trim('/'));
        }
    }
}