using System.ComponentModel.DataAnnotations;
using Octopus.Client.Extensibility.Attributes;
using Octopus.Client.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Client.Model;

namespace Octopus.Client.Extensibility.IssueTracker.AzureDevOps
{
    public class AzureDevOpsConfigurationResource : ExtensionConfigurationResource
    {
        [Writeable]
        public AzureDevOpsConnectionResource[] Connections { get; set; } = new AzureDevOpsConnectionResource[0];
    }

    public class AzureDevOpsConnectionResource
    {
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