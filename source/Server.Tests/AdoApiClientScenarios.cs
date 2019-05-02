using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;

namespace Server.Tests
{
    [TestFixture]
    public class AdoApiClientScenarios
    {
        private static IHttpJsonClient CreateCannedResponseHttpJsonClient()
        {
            var httpJsonClient = Substitute.For<IHttpJsonClient>();
            var httpRequestHeaders = new HttpRequestMessage().Headers;
            httpJsonClient.DefaultRequestHeaders.Returns(httpRequestHeaders);
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/build/builds/24/workitems?api-version=5.0")
                .Returns(JObject.Parse(@"{""count"":1,""value"":[{""id"":""2"",""url"":""http://redstoneblock/DefaultCollection/_apis/wit/workItems/2""}]}"));
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/wit/workitems/2?api-version=5.0")
                .Returns(JObject.Parse(@"{""id"":2,""fields"":{""System.Title"": ""README has no useful content""}}"));
            return httpJsonClient;
        }

        [Test]
        public void ClientCanRequestAndParseWorkItemsRefsAndLinks()
        {
            var store = Substitute.For<IAzureDevOpsConfigurationStore>();
            var httpJsonClient = CreateCannedResponseHttpJsonClient();

            var workItemLink = new AdoApiClient(store, httpJsonClient).GetBuildWorkItemLinks(
                    AdoBuildUrls.ParseBrowserUrl("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24"))
                .Single();

            Assert.AreEqual("2", workItemLink.Id);
            Assert.AreEqual("http://redstoneblock/DefaultCollection/Deployable/_workitems?_a=edit&id=2", workItemLink.LinkUrl);
            Assert.AreEqual("README has no useful content", workItemLink.Description);
        }
    }
}