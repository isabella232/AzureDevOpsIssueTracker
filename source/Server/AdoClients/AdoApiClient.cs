﻿using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public class AdoApiClient
    {
        private const string ApiVersionQuery = @"?api-version=5.0";

        private readonly IAzureDevOpsConfigurationStore store;
        private readonly IHttpJsonClient client;

        public AdoApiClient(IAzureDevOpsConfigurationStore store, IHttpJsonClient client)
        {
            this.store = store;

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var basicAuthCred = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + store.GetPersonalAccessToken()));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthCred);
            this.client = client;
        }

        public (int id, string url)[] GetBuildWorkItemsRefs(AdoBuildUrls adoBuildUrls)
        {
            // ReSharper disable once StringLiteralTypo
            var workItemsUrl = $"{adoBuildUrls.ProjectUrl}/_apis/build/builds/{adoBuildUrls.BuildId}/workitems{ApiVersionQuery}";

            return client.Get(workItemsUrl)["value"]
                .Select(el => (el["id"].Value<int>(), el["url"].ToString()))
                .ToArray();
        }

        internal JObject GetWorkItem(AdoProjectUrls adoProjectUrls, int workItemId)
        {
            // ReSharper disable once StringLiteralTypo
            return client.Get($"{adoProjectUrls.ProjectUrl}/_apis/wit/workitems/{workItemId}{ApiVersionQuery}");
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
                Description = GetWorkItem(adoProjectUrls, workItemId)["fields"]["System.Title"].ToString()
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