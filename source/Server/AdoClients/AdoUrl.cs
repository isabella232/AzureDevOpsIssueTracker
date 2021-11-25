using System;
using System.Web;
using System.Text.RegularExpressions;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    class AdoUrl
    {
        public AdoUrl(string organizationUrl)
        {
            OrganizationUrl = organizationUrl;
        }

        public string OrganizationUrl { get; }
    }

    class AdoProjectUrls : AdoUrl
    {
        public AdoProjectUrls(string organizationUrl) : base(organizationUrl)
        {
        }

        public string? ProjectUrl { get; set; }

        public static AdoProjectUrls ParseOrganizationAndProjectUrls(string organizationOrProjectUrl)
        {
            var uri = new Uri(organizationOrProjectUrl);

            var buildIndex = uri.AbsolutePath.IndexOf("/_build/", StringComparison.OrdinalIgnoreCase);
            if (buildIndex > 0)
            {
                var projectPath = uri.AbsolutePath[..buildIndex];
                var indexBeforeProject = projectPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                var orgPath = projectPath[..indexBeforeProject];
                
                var orgUri = new Uri(uri, orgPath);
                return new AdoProjectUrls(orgUri.AbsoluteUri)
                {
                    ProjectUrl = new Uri(uri, projectPath).AbsoluteUri
                };
            }
            
            if (string.Equals(uri.Host, "dev.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                var chunks = uri.AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries);
                var orgPath = chunks.Length > 0 
                    ? chunks[0] 
                    : null;
                var projectPath = chunks.Length > 1 
                    ? chunks[1].StartsWith('_') || chunks[1].StartsWith('.') 
                        ? null 
                        : chunks[1] 
                    : null;
                
                var orgUri = new Uri(uri, $"/{orgPath}");
                return new AdoProjectUrls(orgUri.AbsoluteUri)
                {
                    ProjectUrl = projectPath != null
                        ? $"{orgUri.AbsoluteUri}/{projectPath}"
                        : null
                };
            }
            
            var match = Regex.Match(uri.AbsolutePath,
                @"^(?<prefix>/[^_./:\\~&%;@'""?<>|#$*][^/:\\~&%;@'""?<>|#$*]*)?/[^_./:\\~&;@'""?<>|#$*][^/:\\~&;@'""?<>|#$*]*");
            var isVsUrl = Regex.IsMatch(uri.Host, @"^[^.]+\.visualstudio\.com$");
            var projectIsSpecified = match.Success
                                     && (match.Groups["prefix"].Success || isVsUrl);
            var collectionPath = match.Groups["prefix"].Success
                ? match.Groups["prefix"].Value
                : isVsUrl /* For VS URLs a single identifier is likely to be a project */
                    ? "/"
                    : match.Value;

            var organizationUrl = new Uri(uri, collectionPath).AbsoluteUri;

            return new AdoProjectUrls(organizationUrl)
            {
                ProjectUrl = projectIsSpecified
                    ? new Uri(uri, match.Value).AbsoluteUri
                    : null
            };
        }
    }

    class AdoBuildUrls : AdoProjectUrls
    {
        public AdoBuildUrls(string organizationUrl, int buildId) : base(organizationUrl)
        {
            BuildId = buildId;
        }

        public int BuildId { get; }

        public static AdoBuildUrls ParseBrowserUrl(string browserUrl)
        {
            ArgumentException ParseError(Exception? innerException = null)
                => new ArgumentException("Unrecognized build browse URL.", nameof(browserUrl), innerException);

            try
            {
                var prefixMatch = Regex.Match(browserUrl, @"^\s*((https?://.+?)/+[^\/]+)/+_build\b");
                if (!prefixMatch.Success)
                {
                    throw ParseError();
                }

                var fullUri = new Uri(browserUrl);
                var queryParams = HttpUtility.ParseQueryString(fullUri.Query);
                var buildId = queryParams["buildId"]!;
                return new AdoBuildUrls(prefixMatch.Groups[2].Value, int.Parse(buildId))
                {
                    ProjectUrl = prefixMatch.Groups[1].Value
                };
            }
            catch (Exception ex)
            {
                throw ParseError(ex);
            }
        }

        public static AdoBuildUrls Create(AdoProjectUrls adoProjectUrls, int buildId)
        {
            return new AdoBuildUrls(adoProjectUrls.OrganizationUrl, buildId) { ProjectUrl = adoProjectUrls.ProjectUrl };
        }
    }
}