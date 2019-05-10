using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Octopus.Data.Resources;
using Octopus.Data.Resources.Attributes;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    [Description("Configure the Azure DevOps Issue Tracker. [Learn more](https://g.octopushq.com/AzureDevOpsIssueTracker).")]
    public class AzureDevOpsConfigurationResource : ExtensionConfigurationResource
    {
        public const string BaseUrlDisplayName = "Azure DevOps Base Url";
        public const string BaseUrlDescription = "Enter the base url of your Azure DevOps organization or collection.";

        [DisplayName(BaseUrlDisplayName)]
        [Description(BaseUrlDescription)]
        [Required]
        [Writeable]
        public string BaseUrl { get; set; }

        public const string PersonalAccessTokenDescription =
            "A Personal Access Token authorized to read scopes 'Build' and 'Work items', added under User Settings.";

        [DisplayName("Personal Access Token")]
        [Description(PersonalAccessTokenDescription)]
        [Writeable]
        public SensitiveValue PersonalAccessToken { get; set; }
    }
}