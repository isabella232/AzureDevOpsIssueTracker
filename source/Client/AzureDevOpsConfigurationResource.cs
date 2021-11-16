using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Octopus.Client.Extensibility.Attributes;
using Octopus.Client.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Client.Model;

namespace Octopus.Client.Extensibility.IssueTracker.AzureDevOps
{
    public class AzureDevOpsConfigurationResource : ExtensionConfigurationResource
    {
        public AzureDevOpsConfigurationResource()
        {
            Id = "issuetracker-azuredevops-v2";
            Connections = new List<AzureDevOpsConnectionResource>();
        }
        
        public IList<AzureDevOpsConnectionResource> Connections { get; }
    }

    public class AzureDevOpsConnectionResource
    {
        public string? Id { get; set; }

        [Required]
        [Writeable]
        public string? BaseUrl { get; set; }

        [Writeable]
        public SensitiveValue? PersonalAccessToken { get; set; }

        public ReleaseNoteOptionsResource ReleaseNoteOptions { get; set; } = new ReleaseNoteOptionsResource();
    }
    
    public class ReleaseNoteOptionsResource
    {
        [Writeable]
        public string? ReleaseNotePrefix { get; set; }
    }
}