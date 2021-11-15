using System.Collections.Generic;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.HostServices.Mapping;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    class AzureDevOpsConfigurationSettings :
        ExtensionConfigurationSettings<AzureDevOpsConfiguration, AzureDevOpsConfigurationResource, IAzureDevOpsConfigurationStore>,
        IAzureDevOpsConfigurationSettings
    {
        public AzureDevOpsConfigurationSettings(IAzureDevOpsConfigurationStore configurationDocumentStore) : base(configurationDocumentStore)
        {
        }

        public override string Id => AzureDevOpsConfigurationStore.SingletonId;

        public override string ConfigurationSetName => "Azure DevOps Issue Tracker";

        public override string Description => "Azure DevOps Issue Tracker settings";

        public override IEnumerable<IConfigurationValue> GetConfigurationValues()
        {
            var isEnabled = ConfigurationDocumentStore.GetIsEnabled();
            yield return new ConfigurationValue<bool>("Octopus.IssueTracker.AzureDevOpsIssueTracker", isEnabled,
                isEnabled, "Is Enabled");
        }

        public override void BuildMappings(IResourceMappingsBuilder builder)
        {
            builder.Map<AzureDevOpsConfigurationResource, AzureDevOpsConfiguration>();
            builder.Map<ReleaseNoteOptionsResource, ReleaseNoteOptions>();
            builder.Map<AzureDevOpsConnectionResource, ReleaseNoteOptions>();
            
        }
    }
}