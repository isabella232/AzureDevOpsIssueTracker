using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Octopus.Data.Resources.Attributes;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    [Description("Configure the Azure DevOps Issue Tracker. [Learn more](https://g.octopushq.com/AzureDevOpsIssueTracker).")]
    public class AzureDevOpsConfigurationResource : ExtensionConfigurationResource
    {
        public const string AzureDevOpsBaseUrlDescription = "Enter the base url of your Azure DevOps organization or collection.";

        [DisplayName("Azure DevOps Base Url")]
        [Description(AzureDevOpsBaseUrlDescription)]
        [Required]
        [Writeable]
        public string BaseUrl { get; set; }
    }
}