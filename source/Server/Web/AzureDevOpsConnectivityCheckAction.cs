using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Web.Response;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Web
{
    public class AzureDevOpsConnectivityCheckAction : IAsyncApiAction
    {
        private readonly IAzureDevOpsConfigurationStore configurationStore;
        private readonly IAdoApiClient adoApiClient;

        public AzureDevOpsConnectivityCheckAction(IAzureDevOpsConfigurationStore configurationStore, IAdoApiClient adoApiClient)
        {
            this.configurationStore = configurationStore;
            this.adoApiClient = adoApiClient;
        }

        public async Task ExecuteAsync(OctoContext context)
        {
            try
            {
                var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<JObject>(json);

                var baseUrl = request.GetValue("BaseUrl").ToString();
                // If PersonalAccessToken here is null, it could be that they're clicking the test connectivity button after saving
                // the configuration as we won't have the value of the PersonalAccessToken on client side, so we need to retrieve it
                // from the database
                var personalAccessToken = request.GetValue("PersonalAccessToken").ToString();
                if (string.IsNullOrEmpty(personalAccessToken))
                {
                    personalAccessToken = configurationStore.GetPersonalAccessToken();
                }

                if (string.IsNullOrEmpty(baseUrl))
                {
                    context.Response.AsOctopusJson(ConnectivityCheckResponse.Failure("Please provide a value for Azure DevOps Base Url."));
                    return;
                }

                var urls = AdoProjectUrls.ParseOrganizationAndProjectUrls(baseUrl);
                AdoProjectUrls[] projectUrls;
                if (urls.ProjectUrl != null)
                {
                    projectUrls = new[] {urls};
                }
                else
                {
                    var projects = adoApiClient.GetProjectList(urls, personalAccessToken, true);
                    if (!projects.Succeeded)
                    {
                        context.Response.AsOctopusJson(ConnectivityCheckResponse.Failure(projects.FailureReason));
                        return;
                    }

                    if (!projects.Value.Any())
                    {
                        context.Response.AsOctopusJson(
                            ConnectivityCheckResponse.Failure("Successfully connected, but unable to find any projects to test permissions."));
                        return;
                    }

                    projectUrls = projects.Value.Select(project => new AdoProjectUrls
                    {
                        OrganizationUrl = urls.OrganizationUrl,
                        ProjectUrl = $"{urls.OrganizationUrl}/{project}"
                    }).ToArray();
                }

                string lastFailureReason = null;
                foreach (var projectUrl in projectUrls)
                {
                    var builds = adoApiClient.GetBuildList(projectUrl, personalAccessToken, true);
                    if (!builds.Succeeded)
                    {
                        lastFailureReason = builds.FailureReason;
                        continue;
                    }

                    if (!builds.Value.Any())
                    {
                        lastFailureReason = "Successfully connected, but unable to find any builds to test permissions.";
                        continue;
                    }

                    var workItems = adoApiClient.GetBuildWorkItemsRefs(AdoBuildUrls.Create(projectUrl, builds.Value.First()), personalAccessToken, true);
                    if (!workItems.Succeeded)
                    {
                        lastFailureReason = workItems.FailureReason;
                        continue;
                    }

                    context.Response.AsOctopusJson(ConnectivityCheckResponse.Success);
                    return;
                }

                context.Response.AsOctopusJson(ConnectivityCheckResponse.Failure(lastFailureReason));
            }
            catch (Exception ex)
            {
                context.Response.AsOctopusJson(ConnectivityCheckResponse.Failure(ex.ToString()));
            }
        }
    }
}