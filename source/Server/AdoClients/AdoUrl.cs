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

        public static AdoProjectUrls ParseOrganizationAndProjectUrls(string organizationOrProjectUrl)
        {
            var uri = new Uri(organizationOrProjectUrl);

            {
                if (string.Equals(uri.Host, "dev.azure.com", StringComparison.OrdinalIgnoreCase)
                    && Regex.Match(uri.AbsolutePath, @"^/(?<org>[^/]+)(/(?<proj>[^_./:\\~&%;@'""?<>|#$*][^/:\\~&%;@'""?<>|#$*]*))?")
                        is Match match && match.Success)
                {
                    var orgUri = new Uri(uri, $"/{match.Groups["org"].Value}");
                    return new AdoProjectUrls
                    {
                        OrganizationUrl = orgUri.AbsoluteUri,
                        ProjectUrl = match.Groups["proj"].Success
                            ? $"{orgUri.AbsoluteUri}/{match.Groups["proj"].Value}"
                            : null
                    };
                }
            }

            {
                var match = Regex.Match(uri.AbsolutePath,
                    @"^(?<prefix>/[^_./:\\~&%;@'""?<>|#$*][^/:\\~&%;@'""?<>|#$*]*)?/[^_./:\\~&%;@'""?<>|#$*][^/:\\~&%;@'""?<>|#$*]*");
                var isVsUrl = Regex.IsMatch(uri.Host, @"^[^.]+\.visualstudio\.com$");
                var projectIsSpecified = match.Success
                                         && (match.Groups["prefix"].Success || isVsUrl);
                var collectionPath = match.Groups["prefix"].Success
                    ? match.Groups["prefix"].Value
                    : isVsUrl /* For VS URLs a single identifier is likely to be a project */
                        ? "/"
                        : match.Value;

                var organizationUrl = new Uri(uri, collectionPath).AbsoluteUri;

                return new AdoProjectUrls
                {
                    OrganizationUrl = organizationUrl,
                    ProjectUrl = projectIsSpecified
                        ? new Uri(uri, match.Value).AbsoluteUri
                        : null
                };
            }
        }
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

        public static AdoBuildUrls Create(AdoProjectUrls adoProjectUrls, int buildId)
        {
            return new AdoBuildUrls {OrganizationUrl = adoProjectUrls.OrganizationUrl, ProjectUrl = adoProjectUrls.ProjectUrl, BuildId = buildId};
        }
    }
}