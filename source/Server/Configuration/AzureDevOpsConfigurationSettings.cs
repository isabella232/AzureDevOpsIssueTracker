using System.Collections.Generic;
using System.Linq;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.HostServices.Mapping;
using Octopus.Server.MessageContracts;

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
            builder.Map<AzureDevOpsConfigurationResource, AzureDevOpsConfiguration>()
                .DoNotMap(model => model.Connections)
                .EnrichResource((model, resource) => resource.Connections = model.Connections.Select(connection => new AzureDevOpsConnectionResource
                {
                    BaseUrl = connection.BaseUrl,
                    PersonalAccessToken = new SensitiveValue { HasValue = connection.PersonalAccessToken?.Value != null },
                    ReleaseNoteOptions = new ReleaseNoteOptionsResource { ReleaseNotePrefix = connection.ReleaseNoteOptions.ReleaseNotePrefix }
                }).ToArray())
                .EnrichModel((model, resource) => model.Connections = resource.Connections.Select(connectionResource => new AzureDevOpsConnection
                {
                    BaseUrl = connectionResource.BaseUrl,
                    PersonalAccessToken = connectionResource.PersonalAccessToken is { HasValue: true }
                        ? connectionResource.PersonalAccessToken.NewValue.ToSensitiveString()
                        : null,
                    ReleaseNoteOptions = new ReleaseNoteOptions { ReleaseNotePrefix = connectionResource.ReleaseNoteOptions.ReleaseNotePrefix }
                }).ToArray());
        }
    }
}