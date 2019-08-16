using System.Net;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public static class HttpExtensions
    {
        public static bool IsSuccessStatusCode(this HttpStatusCode httpStatusCode)
        {
            if (httpStatusCode >= HttpStatusCode.OK)
                return httpStatusCode <= (HttpStatusCode) 299;
            return false;
        }

        public static string ToDescription(this HttpStatusCode httpStatusCode)
        {
            var description = $"{(int) httpStatusCode} ({httpStatusCode}).";
            if (httpStatusCode == HttpStatusCode.Unauthorized)
            {
                description += " Please confirm the Personal Access Token is configured correctly in Azure DevOps Issue Tracker settings.";
            }
            else if (httpStatusCode == (HttpStatusCode) HttpJsonClientStatus.SigninPage)
            {
                description = "The server returned a sign-in page. Please confirm the Personal Access Token is configured correctly in Azure DevOps Issue Tracker settings.";
            }

            return description;
        }
    }
}