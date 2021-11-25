using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.MessageContracts;
using Octopus.Server.MessageContracts.Attributes;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    [Description("Automatically add links and retrieve release notes from Azure DevOps work items in your Octopus release and deployments. [Learn more](https://g.octopushq.com/AzureDevOpsIssueTracker).")]
    class AzureDevOpsConfigurationResource : ExtensionConfigurationResource
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        [Description("Connect your Octopus instance to one or more Azure DevOps organisations")]
        [DisplayName("Connection")]
        public List<AzureDevOpsConnectionResource> Connections { get; } = new();
    }

    class AzureDevOpsConnectionResource
    {
        const string BaseUrlDisplayName = "Azure DevOps Base Url";
        const string BaseUrlDescription = "Set the base url for the Azure DevOps organization or collection or project.";

        public string? Id { get; set; }
        
        [DisplayName(BaseUrlDisplayName)]
        [Description(BaseUrlDescription)]
        [Required]
        [Writeable]
        public string? BaseUrl { get; set; }

        const string PersonalAccessTokenDescription =
            "A Personal Access Token (PAT) authorized to read scopes 'Build' and 'Work items', added under User Settings.";

        [DisplayName("Personal Access Token")]
        [Description(PersonalAccessTokenDescription)]
        [Writeable]
        [AllowConnectivityCheck("Azure DevOps connection", AzureDevOpsIssueTrackerApi.ApiConnectivityCheck, nameof(BaseUrl), nameof(PersonalAccessToken))]
        public SensitiveValue? PersonalAccessToken { get; set; }

        [DisplayName("Release Note Options")]
        public ReleaseNoteOptionsResource ReleaseNoteOptions { get; set; } = new ReleaseNoteOptionsResource();
    }
    
    class ReleaseNoteOptionsResource
    {
        const string ReleaseNotePrefixDescription =
            "Specify a prefix or leave blank to use the work item's title in the release note. (Optional)";

        [DisplayName("Release Note Prefix")]
        [Description(ReleaseNotePrefixDescription)]
        [Writeable]
        public string? ReleaseNotePrefix { get; set; }
    }
}