using System;
using System.Collections.Generic;
using Octopus.Data.Model;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    class AzureDevOpsConfigureCommands : IContributeToConfigureCommand
    {
        readonly ISystemLog systemLog;
        readonly Lazy<IAzureDevOpsConfigurationStore> azureDevOpsConfiguration;

        public AzureDevOpsConfigureCommands(
            ISystemLog systemLog,
            Lazy<IAzureDevOpsConfigurationStore> azureDevOpsConfiguration)
        {
            this.systemLog = systemLog;
            this.azureDevOpsConfiguration = azureDevOpsConfiguration;
        }

        public IEnumerable<ConfigureCommandOption> GetOptions()
        {
            yield return new ConfigureCommandOption("AzureDevOpsIsEnabled=", "Set whether Azure DevOps issue tracker integration is enabled.",
                v =>
                {
                    var isEnabled = bool.Parse(v);
                    azureDevOpsConfiguration.Value.SetIsEnabled(isEnabled);
                    systemLog.Info($"Azure DevOps Issue Tracker integration IsEnabled set to: {isEnabled}");
                });
        }
    }
}