using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public interface IAdoApiClient
    {
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

        public (string title, int? commentCount) GetWorkItem(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            var (status, jObject) = client.Get($"{adoProjectUrls.ProjectUrl}/_apis/wit/workitems/{workItemId}{ApiVersionQuery}",
                GetPersonalAccessToken(adoProjectUrls));
            if (status == HttpStatusCode.NotFound)
            {
                return (workItemId.ToString(), 0);
            }

            if (!status.IsSuccessStatusCode())
            {
                throw new HttpRequestException($"Error while fetching work item details from Azure DevOps: {status.ToDescription()}");
            }

            var fields = jObject["fields"];
            return (fields["System.Title"].ToString(), fields["System.CommentCount"]?.Value<int>());
        }

        /// <returns>Up to 200 comments on the specified work item.</returns>
        public string[] GetWorkItemComments(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            var (status, jObject) = client.Get($"{adoProjectUrls.ProjectUrl}/_apis/wit/workitems/{workItemId}/comments?api-version=5.0-preview.2",
                GetPersonalAccessToken(adoProjectUrls));

            if (status == HttpStatusCode.NotFound)
            {
                return new string[0];
            }

            if (!status.IsSuccessStatusCode())
            {
                throw new HttpRequestException($"Error while fetching work item comments from Azure DevOps: {status.ToDescription()}");
            }

            return jObject["comments"]
                .Select(c => c["text"].ToString())
                .ToArray();
        }

        public string BuildWorkItemBrowserUrl(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            return $"{adoProjectUrls.ProjectUrl}/_workitems?_a=edit&id={workItemId}";
        }

        public string GetReleaseNote(AdoProjectUrls adoProjectUrls, int workItemId, int? commentCount = null)
        {
            var releaseNotePrefix = store.GetReleaseNotePrefix();

            if (string.IsNullOrWhiteSpace(releaseNotePrefix) || commentCount == 0)
                return null;

            var releaseNoteRegex = new Regex("^" + Regex.Escape(releaseNotePrefix), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Return (last, if multiple found) comment that matched release note prefix
            var releaseNoteComment = GetWorkItemComments(adoProjectUrls, workItemId)
                .LastOrDefault(ct => releaseNoteRegex.IsMatch(ct ?? ""));
            return releaseNoteComment != null
                ? releaseNoteRegex.Replace(releaseNoteComment, "").Trim()
                : null;
        }

        public WorkItemLink GetWorkItemLink(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            var (workItemTitle, commentCount) = GetWorkItem(adoProjectUrls, workItemId);

            return new WorkItemLink
            {
                Id = workItemId.ToString(),
                LinkUrl = BuildWorkItemBrowserUrl(adoProjectUrls, workItemId),
                Description = GetReleaseNote(adoProjectUrls, workItemId, commentCount)
                              ?? workItemTitle
                              ?? workItemId.ToString()
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