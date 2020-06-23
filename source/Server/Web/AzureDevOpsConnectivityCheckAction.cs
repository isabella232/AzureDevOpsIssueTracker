using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.Resources.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Web
{
    class AzureDevOpsConnectivityCheckAction : IAsyncApiAction
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
            var connectivityCheckResponse = new ConnectivityCheckResponse();

            try
            {
                var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<JObject>(json);

                var baseUrl = request.GetValue("BaseUrl")?.ToString();
                // If PersonalAccessToken here is null, it could be that they're clicking the test connectivity button after saving
                // the configuration as we won't have the value of the PersonalAccessToken on client side, so we need to retrieve it
                // from the database
                var personalAccessToken = request.GetValue("PersonalAccessToken")?.ToString().ToSensitiveString();
                if (string.IsNullOrEmpty(personalAccessToken?.Value))
                {
                    personalAccessToken = configurationStore.GetPersonalAccessToken();
                }

                if (string.IsNullOrEmpty(baseUrl))
                {
                    connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, "Please provide a value for Azure DevOps Base Url.");
                    context.Response.AsOctopusJson(connectivityCheckResponse);
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
                    var projects = adoApiClient.GetProjectList(urls, personalAccessToken?.Value, true);
                    if (projects.WasFailure)
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, projects.ErrorString);
                        context.Response.AsOctopusJson(connectivityCheckResponse);
                        return;
                    }

                    if (!projects.Value.Any())
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, "Successfully connected, but unable to find any projects to test permissions.");
                        context.Response.AsOctopusJson(connectivityCheckResponse);
                        return;
                    }

                    projectUrls = projects.Value.Select(project => new AdoProjectUrls(urls.OrganizationUrl)
                    {
                        ProjectUrl = $"{urls.OrganizationUrl}/{project}"
                    }).ToArray();
                }

                foreach (var projectUrl in projectUrls)
                {
                    var buildScopeTest = adoApiClient.GetBuildWorkItemsRefs(AdoBuildUrls.Create(projectUrl, 1), personalAccessToken?.Value, true);
                    if (buildScopeTest.WasFailure)
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Warning, buildScopeTest.ErrorString);
                        continue;
                    }

                    var workItemScopeTest = adoApiClient.GetWorkItem(projectUrl, 1, personalAccessToken?.Value, true);
                    if (workItemScopeTest.WasFailure)
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Warning, workItemScopeTest.ErrorString);
                        continue;
                    }

                    // the check has been successful, so ignore any messages that came from previous project checks
                    connectivityCheckResponse = new ConnectivityCheckResponse();
                    connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Info, "The Azure DevOps connection was tested successfully");
                    if (!configurationStore.GetIsEnabled())
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Info, "The Jira Issue Tracker is not enabled, so its functionality will not currently be available");
                        return;
                    }

                    context.Response.AsOctopusJson(connectivityCheckResponse);
                    return;
                }

                context.Response.AsOctopusJson(connectivityCheckResponse);
            }
            catch (Exception ex)
            {
                connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, ex.ToString());
                context.Response.AsOctopusJson(connectivityCheckResponse);
            }
        }
    }
}