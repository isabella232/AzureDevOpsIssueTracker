using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;

namespace Server.Tests
{
    [TestFixture]
    public class CommitLinkMapperScenarios
    {
        private CommitLinkMapper CreateCommitLinkMapper(bool enabled)
        {
            var config = Substitute.For<IAzureDevOpsConfigurationStore>();
            config.GetIsEnabled().Returns(enabled);
            return new CommitLinkMapper(config);
        }

        [Test]
        public void WhenDisabledReturnsNull()
        {
            var link = CreateCommitLinkMapper(false)
                .Map("http://host/git.git", "a165c8b7f93d3970dba331e91d82c2392245828a");
            Assert.IsNull(link);
        }

        [TestCase("http://host/git.git", "a165c8b7f93d3970dba331e91d82c2392245828a",
            ExpectedResult = "http://host/git/commit/a165c8b7f93d3970dba331e91d82c2392245828a")]
        [TestCase("http://redstoneblock/DefaultCollection/Deployable/_git/Deployable", "c16465fc4c106fb499ca9fafc3cb405ac1069e90",
            ExpectedResult = "http://redstoneblock/DefaultCollection/Deployable/_git/Deployable/commit/c16465fc4c106fb499ca9fafc3cb405ac1069e90")]
        public string LinksGetBuilt(string vcsRoot, string commitNumber)
        {
            return CreateCommitLinkMapper(true).Map(vcsRoot, commitNumber);
        }
    }
}