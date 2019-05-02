using System;
using NUnit.Framework;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;

namespace Server.Tests
{
    [TestFixture]
    public class AdoUrlScenarios
    {
        [TestCase("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24",
            "http://redstoneblock/DefaultCollection",
            "http://redstoneblock/DefaultCollection/Deployable",
            24)]
        [TestCase("https://dev.azure.com/Barsonax/Singularity//_build//results?view=logs&buildId=160&lineStart=34&taskId=7dae026f-9e99-5d65-075e-3f7579577f94",
            "https://dev.azure.com/Barsonax",
            "https://dev.azure.com/Barsonax/Singularity",
            160)]
        public void ValidBuildBrowserUrlsAreParsedCorrectly(string browserUrl, string expOrgUrl, string expProjUrl, int expBuildId)
        {
            var abu = AdoBuildUrls.ParseBrowserUrl(browserUrl);
            Assert.AreEqual(expOrgUrl, abu.OrganizationUrl);
            Assert.AreEqual(expProjUrl, abu.ProjectUrl);
            Assert.AreEqual(expBuildId, abu.BuildId);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("http://redstoneblock/DefaultCollection/Deployable")]
        [TestCase("ftp://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24")]
        [TestCase("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=29af")]
        public void InvalidBuildBrowserUrlsThrow(string browserUrl)
        {
            Assert.Throws<ArgumentException>(() => AdoBuildUrls.ParseBrowserUrl(browserUrl));
        }
    }
}