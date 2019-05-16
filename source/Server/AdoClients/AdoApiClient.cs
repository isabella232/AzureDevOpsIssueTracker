using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public interface IAdoApiClient
    {
        ExtResult<WorkItemLink[]> GetBuildWorkItemLinks(AdoBuildUrls adoBuildUrls);
    }

    public class AdoApiClient : IAdoApiClient
    {
        private const string ApiVersionQuery = @"?api-version=5.0";

        private readonly IAzureDevOpsConfigurationStore store;
        private readonly IHttpJsonClient client;
        private readonly HtmlConvert htmlConvert;

        public AdoApiClient(IAzureDevOpsConfigurationStore store, IHttpJsonClient client, HtmlConvert htmlConvert)
        {
            this.store = store;
            this.client = client;
            this.htmlConvert = htmlConvert;
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

        public ExtResult<(int id, string url)[]> GetBuildWorkItemsRefs(AdoBuildUrls adoBuildUrls)
        {
            // ReSharper disable once StringLiteralTypo
            var workItemsUrl = $"{adoBuildUrls.ProjectUrl}/_apis/build/builds/{adoBuildUrls.BuildId}/workitems{ApiVersionQuery}";

            var (status, jObject) = client.Get(workItemsUrl, GetPersonalAccessToken(adoBuildUrls));
            if (status == HttpStatusCode.NotFound)
            {
                return new (int, string)[0];
            }

            if (!status.IsSuccessStatusCode())
            {
                return ExtResult.Failure($"Error while fetching work item references from Azure DevOps: {status.ToDescription()}");
            }

            try
            {
                return jObject["value"]
                    .Select(el => (el["id"].Value<int>(), el["url"].ToString()))
                    .ToArray();
            }
            catch
            {
                return ExtResult.Failure("Unable to interpret work item references from Azure DevOps.");
            }
        }

        public ExtResult<(string title, int? commentCount)> GetWorkItem(AdoProjectUrls adoProjectUrls, int workItemId)
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
                return ExtResult.Failure($"Error while fetching work item details from Azure DevOps: {status.ToDescription()}");
            }

            try
            {
                var fields = jObject["fields"];
                return (fields["System.Title"].ToString(), fields["System.CommentCount"]?.Value<int>());
            }
            catch
            {
                return ExtResult.Failure("Unable to interpret work item details from Azure DevOps.");
            }
        }

        /// <returns>Up to 200 comments on the specified work item.</returns>
        public ExtResult<string[]> GetWorkItemComments(AdoProjectUrls adoProjectUrls, int workItemId)
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
                return ExtResult.Failure($"Error while fetching work item comments from Azure DevOps: {status.ToDescription()}");
            }

            string[] commentsHtml;
            try
            {
                commentsHtml = jObject["comments"]
                    .Select(c => c["text"].ToString())
                    .ToArray();
            }
            catch
            {
                return ExtResult.Failure("Unable to interpret work item comments from Azure DevOps.");
            }

            return commentsHtml
                .Select(h => htmlConvert.ToPlainText(h))
                .Select(t => t?.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();
        }

        public string BuildWorkItemBrowserUrl(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            return $"{adoProjectUrls.ProjectUrl}/_workitems?_a=edit&id={workItemId}";
        }

        public ExtResult<string> GetReleaseNote(AdoProjectUrls adoProjectUrls, int workItemId, int? commentCount = null)
        {
            var releaseNotePrefix = store.GetReleaseNotePrefix();
            if (string.IsNullOrWhiteSpace(releaseNotePrefix) || commentCount == 0)
            {
                return null;
            }

            var comments = GetWorkItemComments(adoProjectUrls, workItemId);
            if (!comments.Succeeded)
            {
                return ExtResult.Failure(comments);
            }

            var releaseNoteRegex = new Regex("^" + Regex.Escape(releaseNotePrefix), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Return (last, if multiple found) comment that matched release note prefix
            var releaseNoteComment = comments.Value
                ?.LastOrDefault(ct => releaseNoteRegex.IsMatch(ct ?? ""));
            var releaseNote = releaseNoteComment != null
                ? releaseNoteRegex.Replace(releaseNoteComment, "").Trim()
                : null;
            return releaseNote;
        }

        public ExtResult<WorkItemLink> GetWorkItemLink(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            var workItem = GetWorkItem(adoProjectUrls, workItemId);
            var releaseNote = GetReleaseNote(adoProjectUrls, workItemId, workItem.Value.commentCount);

            var workItemLink = new WorkItemLink
            {
                Id = workItemId.ToString(),
                LinkUrl = BuildWorkItemBrowserUrl(adoProjectUrls, workItemId),
                Description = !string.IsNullOrWhiteSpace(releaseNote.Value)
                    ? releaseNote.Value
                    : !string.IsNullOrWhiteSpace(workItem.Value.title)
                        ? workItem.Value.title
                        : workItemId.ToString()
            };

            return ExtResult.Conditional(workItemLink, workItem, releaseNote);
        }

        public ExtResult<WorkItemLink[]> GetBuildWorkItemLinks(AdoBuildUrls adoBuildUrls)
        {
            var workItemsRefs = GetBuildWorkItemsRefs(adoBuildUrls);
            if (!workItemsRefs.Succeeded)
            {
                return ExtResult.Failure(workItemsRefs);
            }

            var workItemLinks = workItemsRefs.Value
                .Select(w => GetWorkItemLink(adoBuildUrls, w.id))
                .ToArray();
            var validWorkItemLinks = workItemLinks
                .Select(r => r.Value)
                .Where(v => v != null)
                .ToArray();
            return ExtResult.Conditional(validWorkItemLinks, workItemLinks);
        }
    }
}