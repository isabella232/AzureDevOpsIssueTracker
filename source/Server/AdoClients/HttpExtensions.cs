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
            return $"{(int) httpStatusCode} ({httpStatusCode})";
        }
    }
}