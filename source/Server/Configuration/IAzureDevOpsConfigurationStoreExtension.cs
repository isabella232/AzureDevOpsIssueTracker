using System;
using System.Collections.Generic;
using System.Linq;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    static class IAzureDevOpsConfigurationStoreExtension
    {
        public static AzureDevOpsConnection? GetMostQualifiedConnection(this IAzureDevOpsConfigurationStore store, AdoProjectUrls adoProjectUrls)
        {
            var copyList = new List<AzureDevOpsConnection>(store.GetConnections().Select(connection =>
            {
                if (connection.BaseUrl == null)
                {
                    return connection;
                }
                
                if (!connection.BaseUrl.EndsWith("/"))
                {
                    connection.BaseUrl += "/";
                }
                
                return connection;
            }));

            copyList.Sort((connection1, connection2) =>
            {
                if (connection1.BaseUrl == connection2.BaseUrl)
                {
                    return 0;
                }

                if (connection1.BaseUrl?.Length > connection2.BaseUrl?.Length)
                {
                    return -1;
                }

                return 1;
            });

            return copyList.FirstOrDefault(connection =>
            {
                if (connection.BaseUrl == null)
                {
                    return false;
                }

                var baseUrl = new Uri(connection.BaseUrl, UriKind.Absolute);

                var url = adoProjectUrls.ProjectUrl ?? adoProjectUrls.OrganizationUrl;
                if (!url.EndsWith("/"))
                {
                    url += "/";
                }
                
                return baseUrl.IsBaseOf(new Uri(url, UriKind.Absolute));
            });
        }
    }
}