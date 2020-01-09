using System;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Web;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps
{
    public class AzureDevOpsIssueTrackerApi : RegisterEndpoint
    {
        public const string ApiConnectivityCheck = "/api/azuredevopsissuetracker/connectivitycheck";

        public AzureDevOpsIssueTrackerApi(
            Func<SecuredAsyncActionInvoker<AzureDevOpsConnectivityCheckAction>> azureDevOpsConnectivityCheckInvokerFactory)
        {
            Add("POST", ApiConnectivityCheck, azureDevOpsConnectivityCheckInvokerFactory().ExecuteAsync);
        }
    }
}