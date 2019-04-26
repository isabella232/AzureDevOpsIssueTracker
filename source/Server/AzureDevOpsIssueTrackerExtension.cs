﻿using Autofac;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.Extensions.Infrastructure;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.Extensions.Mappings;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps
{
    [OctopusPlugin("Azure DevOps Issue Tracker", "Octopus Deploy")]
    public class AzureDevOpsIssueTrackerExtension : IOctopusExtension
    {
        public void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzureDevOpsConfigurationMapping>()
                .As<IConfigurationDocumentMapper>()
                .InstancePerLifetimeScope();

            builder.RegisterType<DatabaseInitializer>()
                .As<IExecuteWhenDatabaseInitializes>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AzureDevOpsConfigurationStore>()
                .As<IAzureDevOpsConfigurationStore>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AzureDevOpsConfigurationSettings>()
                .As<IAzureDevOpsConfigurationSettings>()
                .As<IHasConfigurationSettings>()
                .As<IHasConfigurationSettingsResource>()
                .As<IContributeMappings>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AzureDevOpsIssueTracker>()
                .As<IIssueTracker>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AzureDevOpsConfigureCommands>()
                .As<IContributeToConfigureCommand>()
                .InstancePerDependency();
        }
    }
}