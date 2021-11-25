using System;
using System.Linq;
using System.Threading.Tasks;
using Octopus.Data;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.Resources.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Web
{
    class AzureDevOpsConnectivityCheckAction : IAsyncApiAction
    {
        static readonly RequestBodyRegistration<ConnectionCheckData> Data = new RequestBodyRegistration<ConnectionCheckData>();
        static readonly OctopusJsonRegistration<ConnectivityCheckResponse> Result = new OctopusJsonRegistration<ConnectivityCheckResponse>();

        private readonly IAzureDevOpsConfigurationStore configurationStore;
        private readonly IAdoApiClient adoApiClient;

        public AzureDevOpsConnectivityCheckAction(IAzureDevOpsConfigurationStore configurationStore, IAdoApiClient adoApiClient)
        {
            this.configurationStore = configurationStore;
            this.adoApiClient = adoApiClient;
        }

        public Task<IOctoResponseProvider> ExecuteAsync(IOctoRequest request)
        {
            var connectivityCheckResponse = new ConnectivityCheckResponse();

            try
            {
                var requestData = request.GetBody(Data);

                var baseUrl = requestData.BaseUrl;
                // If PersonalAccessToken here is null, it could be that they're clicking the test connectivity button after saving
                // the configuration as we won't have the value of the PersonalAccessToken on client side, so we need to retrieve it
                // from the database
                var personalAccessToken = requestData.PersonalAccessToken.ToSensitiveString();
                if (string.IsNullOrEmpty(personalAccessToken?.Value))
                {
                    personalAccessToken = configurationStore.GetConnections().FirstOrDefault(connection => connection.BaseUrl == baseUrl)?.PersonalAccessToken;
                }

                if (string.IsNullOrEmpty(baseUrl))
                {
                    connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, "Please provide a value for Azure DevOps Base Url.");
                    return Task.FromResult(Result.Response(connectivityCheckResponse));
                }

                var urls = AdoProjectUrls.ParseOrganizationAndProjectUrls(baseUrl);
                AdoProjectUrls[] projectUrls;
                if (urls.ProjectUrl != null)
                {
                    projectUrls = new[] {urls};
                }
                else
                {
                    var projectsResult = adoApiClient.GetProjectList(urls, personalAccessToken?.Value);
                    if (projectsResult is FailureResult failure)
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, failure.ErrorString);
                        return Task.FromResult(Result.Response(connectivityCheckResponse));
                    }

                    var projects = (ISuccessResult<string[]>) projectsResult;

                    if (!projects.Value.Any())
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, "Successfully connected, but unable to find any projects to test permissions.");
                        return Task.FromResult(Result.Response(connectivityCheckResponse));
                    }

                    projectUrls = projects.Value.Select(project => new AdoProjectUrls(urls.OrganizationUrl)
                    {
                        ProjectUrl = $"{urls.OrganizationUrl}/{project}"
                    }).ToArray();
                }

                var hasError = false;
                foreach (var projectUrl in projectUrls)
                {
                    var buildScopeTest = adoApiClient.CheckWeCanGetBuilds(projectUrl, personalAccessToken?.Value);
                    if (buildScopeTest is FailureResult buildScopeFailure)
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, buildScopeFailure.ErrorString);
                        hasError = true;
                    }
                }

                if (!hasError)
                {
                    connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Info, "Successfully connected to Azure DevOps");

                    if (!configurationStore.GetIsEnabled())
                    {
                        connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Warning, "The Jira Issue Tracker is not enabled, so its functionality will not currently be available");
                    }
                }
                
                return Task.FromResult(Result.Response(connectivityCheckResponse));
            }
            catch (Exception ex)
            {
                connectivityCheckResponse.AddMessage(ConnectivityCheckMessageCategory.Error, ex.ToString());
                return Task.FromResult(Result.Response(connectivityCheckResponse));
            }
        }
    }

#nullable disable
    class ConnectionCheckData
    {
        public string BaseUrl { get; set; }
        public string PersonalAccessToken { get; set; }
    }
}
