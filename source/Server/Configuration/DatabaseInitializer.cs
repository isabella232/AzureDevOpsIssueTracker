using Octopus.Data.Storage.Configuration;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration
{
    class DatabaseInitializer : ExecuteWhenDatabaseInitializes
    {
        readonly ISystemLog log;
        readonly IConfigurationStore configurationStore;

        public DatabaseInitializer(ISystemLog log, IConfigurationStore configurationStore)
        {
            this.log = log;
            this.configurationStore = configurationStore;
        }

        public override void Execute()
        {
            var doc = configurationStore.Get<AzureDevOpsConfiguration>(AzureDevOpsConfigurationStore.SingletonId);
            if (doc != null)
                return;

            log.Info("Initializing Azure DevOps integration settings");
            doc = new AzureDevOpsConfiguration();
            configurationStore.Create(doc);
        }
    }
}