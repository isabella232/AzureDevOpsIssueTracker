using System;
using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.HostServices.Model.PackageMetadata;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
        private WorkItemLinkMapper CreateWorkItemLinkMapper(bool enabled)
        {
            var config = Substitute.For<IAzureDevOpsConfigurationStore>();
            config.GetIsEnabled().Returns(enabled);
            var adoApiClient = Substitute.For<IAdoApiClient>();
            adoApiClient.GetBuildWorkItemLinks(null).ReturnsForAnyArgs(ci => throw new InvalidOperationException());
            return new WorkItemLinkMapper(config, adoApiClient);
        }

        [Test]
        public void WhenDisabledReturnsNull()
        {
            var links = CreateWorkItemLinkMapper(false).Map(new OctopusPackageMetadata
            {
                BuildUrl = "http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24",
                CommentParser = AzureDevOpsConfigurationStore.CommentParser
            });
            Assert.IsTrue(links.Succeeded);
            Assert.IsNull(links.Value);
        }

        [Test]
        public void DoesNotAttemptToMapOtherCommentParsers()
        {
            // ReSharper disable once StringLiteralTypo
            var links = CreateWorkItemLinkMapper(true).Map(new OctopusPackageMetadata
            {
                BuildUrl = "http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24",
                CommentParser = "Jira"
            });
            Assert.IsTrue(links.Succeeded);
            Assert.IsNull(links.Value);
        }
    }
}