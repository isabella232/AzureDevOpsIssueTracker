using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public interface IAdoApiClient
    {
        (int id, string url)[] GetBuildWorkItemsRefs(AdoBuildUrls adoBuildUrls);
        string BuildWorkItemBrowserUrl(AdoProjectUrls adoProjectUrls, int workItemId);
        WorkItemLink GetWorkItemLink(AdoProjectUrls adoProjectUrls, int workItemId);
        WorkItemLink[] GetBuildWorkItemLinks(AdoBuildUrls adoBuildUrls);
    }

    public class AdoApiClient : IAdoApiClient
    {
        private const string ApiVersionQuery = @"?api-version=5.0";

        private readonly IAzureDevOpsConfigurationStore store;
        private readonly IHttpJsonClient client;

        public AdoApiClient(IAzureDevOpsConfigurationStore store, IHttpJsonClient client)
        {
            this.store = store;
            this.client = client;
        }

        internal string GetPersonalAccessToken(AdoUrl adoUrl)
        {
            try
            {
                return new Uri(store.GetBaseUrl().TrimEnd('/'), UriKind.Absolute).IsBaseOf(new Uri(adoUrl.OrganizationUrl, UriKind.Absolute))
                    ? store.GetPersonalAccessToken()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        public (int id, string url)[] GetBuildWorkItemsRefs(AdoBuildUrls adoBuildUrls)
        {
            // ReSharper disable once StringLiteralTypo
            var workItemsUrl = $"{adoBuildUrls.ProjectUrl}/_apis/build/builds/{adoBuildUrls.BuildId}/workitems{ApiVersionQuery}";

            var (status, jObject) = client.Get(workItemsUrl, GetPersonalAccessToken(adoBuildUrls));
            if (status == HttpStatusCode.NotFound)
            {
                return new (int id, string url)[0];
            }

            if (!status.IsSuccessStatusCode())
            {
                throw new HttpRequestException($"Error while fetching work items from Azure DevOps: {status.ToDescription()}");
            }

            return jObject["value"]
                .Select(el => (el["id"].Value<int>(), el["url"].ToString()))
                .ToArray();
        }

        internal string GetWorkItemTitle(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            var (status, jObject) = client.Get($"{adoProjectUrls.ProjectUrl}/_apis/wit/workitems/{workItemId}{ApiVersionQuery}",
                GetPersonalAccessToken(adoProjectUrls));
            if (status == HttpStatusCode.NotFound)
            {
                return workItemId.ToString();
            }

            if (!status.IsSuccessStatusCode())
            {
                throw new HttpRequestException($"Error while fetching work item details from Azure DevOps: {status.ToDescription()}");
            }

            return jObject["fields"]["System.Title"].ToString();
        }

        public string BuildWorkItemBrowserUrl(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            return $"{adoProjectUrls.ProjectUrl}/_workitems?_a=edit&id={workItemId}";
        }

        public WorkItemLink GetWorkItemLink(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            return new WorkItemLink
            {
                Id = workItemId.ToString(),
                LinkUrl = BuildWorkItemBrowserUrl(adoProjectUrls, workItemId),
                Description = GetWorkItemTitle(adoProjectUrls, workItemId)
            };
        }

        public WorkItemLink[] GetBuildWorkItemLinks(AdoBuildUrls adoBuildUrls)
        {
            return GetBuildWorkItemsRefs(adoBuildUrls)
                .Select(w => GetWorkItemLink(adoBuildUrls, w.id))
                .ToArray();
        }
    }
}