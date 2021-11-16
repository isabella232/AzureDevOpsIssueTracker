using System;
using System.Collections.Generic;
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
                .DoNotMap(configuration => configuration.Connections)
                .EnrichResource((configuration, resource) =>
                {
                    foreach (var connection in configuration.Connections)
                    {
                        resource.Connections.Add(new AzureDevOpsConnectionResource
                        {
                            Id = connection.Id,
                            BaseUrl = connection.BaseUrl,
                            ReleaseNoteOptions = new ReleaseNoteOptionsResource { ReleaseNotePrefix = connection.ReleaseNoteOptions.ReleaseNotePrefix },
                            PersonalAccessToken = new SensitiveValue { HasValue = connection.PersonalAccessToken?.Value != null }
                        });
                    }
                })
                .EnrichModel((configuration, resource) =>
                {
                    var copyConnection = new List<AzureDevOpsConnection>(configuration.Connections);
                    
                    configuration.Connections.Clear();
                    
                    foreach (var connectionResource in resource.Connections)
                    {
                        var item = connectionResource.Id != null 
                            ? copyConnection.Find(connection => connection.Id == connectionResource.Id) 
                            : null;
                        
                        if (item != null)
                        {
                            if (connectionResource.PersonalAccessToken != null && connectionResource.PersonalAccessToken.HasValue && connectionResource.PersonalAccessToken.NewValue != null)
                            {
                                item.PersonalAccessToken = connectionResource.PersonalAccessToken.NewValue.ToSensitiveString();
                            }
                        }
                        else
                        {
                            item = new AzureDevOpsConnection
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                PersonalAccessToken = connectionResource.PersonalAccessToken?.NewValue.ToSensitiveString()
                            };
                        }
                        
                        item.BaseUrl = connectionResource.BaseUrl;
                        item.ReleaseNoteOptions.ReleaseNotePrefix = connectionResource.ReleaseNoteOptions.ReleaseNotePrefix;
                        
                        configuration.Connections.Add(item);
                    }
                });
            
        }
    }
}