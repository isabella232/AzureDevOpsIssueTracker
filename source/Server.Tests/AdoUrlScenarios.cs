using System;
using NUnit.Framework;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Tests
{
    [TestFixture]
    public class AdoUrlScenarios
    {
        [TestCase("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24",
            "http://redstoneblock/DefaultCollection",
            "http://redstoneblock/DefaultCollection/Deployable",
            24)]
        [TestCase("https://dev.azure.com/Barsonax/Singularity/_build/results?view=logs&buildId=160&lineStart=34&taskId=7dae026f-9e99-5d65-075e-3f7579577f94",
            "https://dev.azure.com/Barsonax",
            "https://dev.azure.com/Barsonax/Singularity",
            160)]
        [TestCase("https://dev.azure.com/Barsonax/Project%20with%20Spaces/_build/results?view=logs&buildId=160&lineStart=34&taskId=7dae026f-9e99-5d65-075e-3f7579577f94",
            "https://dev.azure.com/Barsonax",
            "https://dev.azure.com/Barsonax/Project%20with%20Spaces",
            160)]
        public void ValidBuildBrowserUrlsAreParsedCorrectly(string browserUrl, string expectedOrgUrl, string expectedProjUrl, int expectedBuildId)
        {
            var abu = AdoBuildUrls.ParseBrowserUrl(browserUrl);
            Assert.AreEqual(expectedOrgUrl, abu.OrganizationUrl);
            Assert.AreEqual(expectedProjUrl, abu.ProjectUrl);
            Assert.AreEqual(expectedBuildId, abu.BuildId);
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

        [TestCase(
            "https://dev.azure.com/octopus-deploy-test/_apis/projects?api-version=4.1",
            "https://dev.azure.com/octopus-deploy-test", null)]
        [TestCase(
            "https://dev.azure.com/octopus-deploy-test/VSTS.Extensions.TestProject/_apis/build/builds?api-version=4.1",
            "https://dev.azure.com/octopus-deploy-test",
            "https://dev.azure.com/octopus-deploy-test/VSTS.Extensions.TestProject")]
        [TestCase(
            "https://dev.azure.com/octopus-deploy-test/My%20first%20project/_apis/build/builds?api-version=4.1",
            "https://dev.azure.com/octopus-deploy-test",
            "https://dev.azure.com/octopus-deploy-test/My%20first%20project")]
        [TestCase(
            "https://octopus-deploy-test.visualstudio.com/_apis/projects?api-version=4.1",
            "https://octopus-deploy-test.visualstudio.com/", null)]
        [TestCase(
            "https://octopus-deploy-test.visualstudio.com/VSTS.Extensions.TestProject/_apis/build/builds?api-version=4.1",
            "https://octopus-deploy-test.visualstudio.com/",
            "https://octopus-deploy-test.visualstudio.com/VSTS.Extensions.TestProject")]
        [TestCase(
            "https://octopus-deploy-test.visualstudio.com/Project%20with%20Spaces/_apis/build/builds?api-version=4.1",
            "https://octopus-deploy-test.visualstudio.com/",
            "https://octopus-deploy-test.visualstudio.com/Project%20with%20Spaces")]
        [TestCase(
            "https://octopus-deploy-test.visualstudio.com/DefaultCollection/VSTS.Extensions.TestProject/_apis/build/builds?api-version=4.1",
            "https://octopus-deploy-test.visualstudio.com/DefaultCollection",
            "https://octopus-deploy-test.visualstudio.com/DefaultCollection/VSTS.Extensions.TestProject")]
        [TestCase(
            "https://octopus-deploy-test.visualstudio.com/DefaultCollection/Project%20with%20Spaces/_apis/build/builds?api-version=4.1",
            "https://octopus-deploy-test.visualstudio.com/DefaultCollection",
            "https://octopus-deploy-test.visualstudio.com/DefaultCollection/Project%20with%20Spaces")]
        [TestCase(
            "http://redstoneblock/DefaultCollection/_apis/projects?api-version=4.1",
            "http://redstoneblock/DefaultCollection", null)]
        [TestCase(
            "http://redstoneblock/DefaultCollection/Deployable/_apis/build/builds?api-version=4.1",
            "http://redstoneblock/DefaultCollection",
            "http://redstoneblock/DefaultCollection/Deployable")]
        [TestCase(
            "http://redstoneblock/DefaultCollection/Project%20with%20Spaces/_apis/build/builds?api-version=4.1",
            "http://redstoneblock/DefaultCollection",
            "http://redstoneblock/DefaultCollection/Project%20with%20Spaces")]
        [TestCase(
            "http://redstoneblock/DefaultCollection/e120aadd-7a70-4267-9a59-e32c3ebf4e8f/_apis/build/builds?api-version=4.1",
            "http://redstoneblock/DefaultCollection",
            "http://redstoneblock/DefaultCollection/e120aadd-7a70-4267-9a59-e32c3ebf4e8f")]
        public void ValidOrganizationAndProjectUrlsAreParsedCorrectly(string organizationOrProjectUrl, string expectedOrgUrl, string expectedProjUrl)
        {
            var apu = AdoProjectUrls.ParseOrganizationAndProjectUrls(organizationOrProjectUrl);
            Assert.AreEqual(expectedOrgUrl, apu.OrganizationUrl);
            Assert.AreEqual(expectedProjUrl, apu.ProjectUrl);
        }
    }
}