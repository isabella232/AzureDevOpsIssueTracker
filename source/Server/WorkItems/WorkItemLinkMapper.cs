using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.HostServices.Model.PackageMetadata;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems
{
    public class WorkItemLinkMapper : IWorkItemLinkMapper
    {
        private readonly IAzureDevOpsConfigurationStore store;
        private readonly IAdoApiClient client;

        public WorkItemLinkMapper(IAzureDevOpsConfigurationStore store, IAdoApiClient client)
        {
            this.store = store;
            this.client = client;
        }

        public string CommentParser => AzureDevOpsConfigurationStore.CommentParser;
        public bool IsEnabled => store.GetIsEnabled();

        public WorkItemLink[] Map(OctopusPackageMetadata packageMetadata)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(packageMetadata.BuildUrl))
                return null;

            return client.GetBuildWorkItemLinks(AdoBuildUrls.ParseBrowserUrl(packageMetadata.BuildUrl));
        }
    }
}