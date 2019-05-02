using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public class AdoUrl
    {
        public string OrganizationUrl { get; set; }
    }

    public class AdoProjectUrls : AdoUrl
    {
        public string ProjectUrl { get; set; }
    }

    public class AdoBuildUrls : AdoProjectUrls
    {
        public int BuildId { get; set; }

        public static AdoBuildUrls ParseBrowserUrl(string browserUrl)
        {
            ArgumentException ParseError(Exception innerException = null)
                => new ArgumentException("Unrecognized build browse URL.", nameof(browserUrl), innerException);

            try
            {
                var prefixMatch = Regex.Match(browserUrl, @"^\s*((https?://.+?)/+[^\/]+)/+_build\b");
                if (!prefixMatch.Success)
                {
                    throw ParseError();
                }

                return new AdoBuildUrls
                {
                    OrganizationUrl = prefixMatch.Groups[2].Value,
                    ProjectUrl = prefixMatch.Groups[1].Value,
                    BuildId = int.Parse(new Uri(browserUrl, UriKind.Absolute).ParseQueryString()["buildId"])
                };
            }
            catch (Exception ex)
            {
                throw ParseError(ex);
            }
        }
    }
}