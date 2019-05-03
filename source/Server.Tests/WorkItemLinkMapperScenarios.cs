using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.HostServices.Model.PackageMetadata;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;

namespace Server.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
        private WorkItemLinkMapper CreateWorkItemLinkMapper(bool enabled)
        {
            var config = Substitute.For<IAzureDevOpsConfigurationStore>();
            config.GetIsEnabled().Returns(enabled);
            return new WorkItemLinkMapper(config, Substitute.For<IAdoApiClient>());
        }

        [Test]
        public void WhenDisabledReturnsNull()
        {
            var link = CreateWorkItemLinkMapper(false)
                .Map(new OctopusPackageMetadata {BuildUrl = "http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24"});
            Assert.IsNull(link);
        }
    }
}