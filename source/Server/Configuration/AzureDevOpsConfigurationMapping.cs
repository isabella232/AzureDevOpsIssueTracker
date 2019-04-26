using System;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    public class AzureDevOpsConfigurationMapping : IConfigurationDocumentMapper
    {
        public Type GetTypeToMap() => typeof(AzureDevOpsConfiguration);
    }
}