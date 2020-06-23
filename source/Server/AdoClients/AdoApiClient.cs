using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;
using Octopus.Server.Extensibility.Resources.IssueTrackers;
using Octopus.Server.Extensibility.Results;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    interface IAdoApiClient
    {
        ResultFromExtension<(int id, string url)[]> GetBuildWorkItemsRefs(AdoBuildUrls adoBuildUrls, string? personalAccessToken = null, bool testing = false);

        ResultFromExtension<(string title, int? commentCount)> GetWorkItem(AdoProjectUrls adoProjectUrls, int workItemId, string? personalAccessToken = null,
            bool testing = false);

        ResultFromExtension<WorkItemLink[]> GetBuildWorkItemLinks(AdoBuildUrls adoBuildUrls);
        ResultFromExtension<string[]> GetProjectList(AdoUrl adoUrl, string? personalAccessToken = null, bool testing = false);
    }

    class AdoApiClient : IAdoApiClient
    {
        private readonly IAzureDevOpsConfigurationStore store;
        private readonly IHttpJsonClient client;
        private readonly HtmlConvert htmlConvert;

        public AdoApiClient(IAzureDevOpsConfigurationStore store, IHttpJsonClient client, HtmlConvert htmlConvert)
        {
            this.store = store;
            this.client = client;
            this.htmlConvert = htmlConvert;
        }

        internal string? GetPersonalAccessToken(AdoUrl adoUrl)
        {
            try
            {
                var baseUrl = store.GetBaseUrl();
                return baseUrl == null ? null : 
                    new Uri(baseUrl?.TrimEnd('/'), UriKind.Absolute).IsBaseOf(new Uri(adoUrl.OrganizationUrl, UriKind.Absolute))
                    ? store.GetPersonalAccessToken()?.Value
                    : null;
            }
            catch
            {
                return null;
            }
        }

        public ResultFromExtension<(int id, string url)[]> GetBuildWorkItemsRefs(AdoBuildUrls adoBuildUrls, string? personalAccessToken = null,
            bool testing = false)
        {
            // ReSharper disable once StringLiteralTypo
            var workItemsUrl = $"{adoBuildUrls.ProjectUrl}/_apis/build/builds/{adoBuildUrls.BuildId}/workitems?api-version=4.1";

            var (status, jObject) = client.Get(workItemsUrl, personalAccessToken ?? GetPersonalAccessToken(adoBuildUrls));
            if (status.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return ResultFromExtension<(int id, string url)[]>.Success(new (int, string)[0]);
            }

            if (!status.IsSuccessStatusCode())
            {
                return ResultFromExtension<(int id, string url)[]>.Failed($"Error while fetching work item references from Azure DevOps: {status.ToDescription(jObject, testing)}");
            }

            try
            {
                return ResultFromExtension<(int id, string url)[]>.Success(jObject?["value"]?
                    .Select(el => (el["id"]?.Value<int>() ?? default(int), el["url"]?.ToString() ?? string.Empty))
                    .ToArray() ?? Array.Empty<(int id, string url)>());
            }
            catch
            {
                return ResultFromExtension<(int id, string url)[]>.Failed("Unable to interpret work item references from Azure DevOps.");
            }
        }

        public ResultFromExtension<(string title, int? commentCount)> GetWorkItem(AdoProjectUrls adoProjectUrls, int workItemId,
            string? personalAccessToken = null, bool testing = false)
        {
            // ReSharper disable once StringLiteralTypo
            var (status, jObject) = client.Get($"{adoProjectUrls.ProjectUrl}/_apis/wit/workitems/{workItemId}?api-version=4.1",
                personalAccessToken ?? GetPersonalAccessToken(adoProjectUrls));
            if (status.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return ResultFromExtension<(string title, int? commentCount)>.Success((workItemId.ToString(), 0));
            }

            if (!status.IsSuccessStatusCode())
            {
                return ResultFromExtension<(string title, int? commentCount)>.Failed($"Error while fetching work item details from Azure DevOps: {status.ToDescription(jObject, testing)}");
            }

            try
            {
                var fields = jObject?["fields"];
                if (fields == null)
                    return ResultFromExtension<(string title, int? commentCount)>.Failed("Unable to interpret work item details from Azure DevOps. `fields` element is missing.");

                return ResultFromExtension<(string title, int? commentCount)>.Success((fields?["System.Title"]?.ToString() ?? string.Empty, fields?["System.CommentCount"]?.Value<int>() ?? default(int)));
            }
            catch
            {
                return ResultFromExtension<(string title, int? commentCount)>.Failed("Unable to interpret work item details from Azure DevOps.");
            }
        }

        /// <returns>Up to 200 comments on the specified work item.</returns>
        public ResultFromExtension<string[]> GetWorkItemComments(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            var (status, jObject) = client.Get($"{adoProjectUrls.ProjectUrl}/_apis/wit/workitems/{workItemId}/comments?api-version=4.1-preview.2",
                GetPersonalAccessToken(adoProjectUrls));

            if (status.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return ResultFromExtension<string[]>.Success(new string[0]);
            }

            if (!status.IsSuccessStatusCode())
            {
                return ResultFromExtension<string[]>.Failed($"Error while fetching work item comments from Azure DevOps: {status.ToDescription(jObject)}");
            }

            string[] commentsHtml;
            try
            {
                commentsHtml = jObject?["comments"]?
                    .Select(c => c["text"]?.ToString())
                    .Where(c => c != null)
                    .Cast<string>() // cast to keep the compiler happy with nullable checks
                    .ToArray() ?? Array.Empty<string>();
            }
            catch
            {
                return ResultFromExtension<string[]>.Failed("Unable to interpret work item comments from Azure DevOps.");
            }

            return ResultFromExtension<string[]>.Success(commentsHtml
                .Select(h => htmlConvert.ToPlainText(h))
                .Select(t => t?.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Cast<string>() // cast to keep the compiler happy with nullable checks
                .ToArray());
        }

        public string BuildWorkItemBrowserUrl(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            return $"{adoProjectUrls.ProjectUrl}/_workitems?_a=edit&id={workItemId}";
        }

        public ResultFromExtension<string?> GetReleaseNote(AdoProjectUrls adoProjectUrls, int workItemId, int? commentCount = null)
        {
            var releaseNotePrefix = store.GetReleaseNotePrefix();
            if (string.IsNullOrWhiteSpace(releaseNotePrefix) || commentCount == 0)
            {
                return ResultFromExtension<string?>.Success(null);
            }

            var comments = GetWorkItemComments(adoProjectUrls, workItemId);
            if (comments.WasFailure)
            {
                return ResultFromExtension<string?>.Failed(comments.Errors);
            }

            var releaseNoteRegex = new Regex("^" + Regex.Escape(releaseNotePrefix), RegexOptions.IgnoreCase);
            // Return (last, if multiple found) comment that matched release note prefix
            var releaseNoteComment = comments.Value
                ?.LastOrDefault(ct => releaseNoteRegex.IsMatch(ct ?? ""));
            var releaseNote = releaseNoteComment != null
                ? releaseNoteRegex.Replace(releaseNoteComment, "").Trim()
                : null;
            return ResultFromExtension<string?>.Success(releaseNote);
        }

        public ResultFromExtension<WorkItemLink> GetWorkItemLink(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            var workItem = GetWorkItem(adoProjectUrls, workItemId);
            if (workItem.WasFailure)
                return ResultFromExtension<WorkItemLink>.Failed(workItem.Errors);
            var releaseNote = GetReleaseNote(adoProjectUrls, workItemId, workItem.Value.commentCount);
            if (releaseNote.WasFailure)
                return ResultFromExtension<WorkItemLink>.Failed(releaseNote.Errors);

            var workItemLink = new WorkItemLink
            {
                Id = workItemId.ToString(),
                LinkUrl = BuildWorkItemBrowserUrl(adoProjectUrls, workItemId),
                Description = !string.IsNullOrWhiteSpace(releaseNote.Value)
                    ? releaseNote.Value
                    : !string.IsNullOrWhiteSpace(workItem.Value.title)
                        ? workItem.Value.title
                        : workItemId.ToString(),
                Source = AzureDevOpsConfigurationStore.CommentParser
            };

            return ResultFromExtension<WorkItemLink>.Success(workItemLink);
        }

        public ResultFromExtension<WorkItemLink[]> GetBuildWorkItemLinks(AdoBuildUrls adoBuildUrls)
        {
            var workItemsRefs = GetBuildWorkItemsRefs(adoBuildUrls);
            if (workItemsRefs.WasFailure)
                return ResultFromExtension<WorkItemLink[]>.Failed(workItemsRefs.Errors);

            var workItemLinks = workItemsRefs.Value
                .Select(w => GetWorkItemLink(adoBuildUrls, w.id))
                .ToArray();
            var validWorkItemLinks = workItemLinks
                .Select(r => r.Value)
                .Where(v => v != null)
                .ToArray();
            return ResultFromExtension<WorkItemLink[]>.Success(validWorkItemLinks);
        }

        public ResultFromExtension<string[]> GetProjectList(AdoUrl adoUrl, string? personalAccessToken = null, bool testing = false)
        {
            var (status, jObject) = client.Get($"{adoUrl.OrganizationUrl}/_apis/projects?api-version=4.1",
                personalAccessToken ?? GetPersonalAccessToken(adoUrl));

            if (!status.IsSuccessStatusCode())
            {
                return ResultFromExtension<string[]>.Failed($"Error while fetching project list from Azure DevOps: {status.ToDescription(jObject, testing)}");
            }

            return ResultFromExtension<string[]>.Success(jObject?["value"]?
                .Select(p => p["name"]?.ToString())
                .Cast<string>() // cast to keep the compiler happy with nullable checks
                .ToArray() ?? Array.Empty<string>());
        }
    }
}