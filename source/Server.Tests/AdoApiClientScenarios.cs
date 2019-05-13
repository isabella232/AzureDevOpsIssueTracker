using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Tests
{
    [TestFixture]
    public class AdoApiClientScenarios
    {
        private static IAzureDevOpsConfigurationStore CreateSubstituteStore()
        {
            var store = Substitute.For<IAzureDevOpsConfigurationStore>();
            store.GetBaseUrl().Returns("http://redstoneblock/DefaultCollection/");
            store.GetPersonalAccessToken().Returns("rumor");
            return store;
        }

        [Test]
        public void ClientCanRequestAndParseWorkItemsRefsAndLinks()
        {
            var store = CreateSubstituteStore();
            var httpJsonClient = Substitute.For<IHttpJsonClient>();
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/build/builds/24/workitems?api-version=5.0", "rumor")
                .Returns((HttpStatusCode.OK,
                    JObject.Parse(@"{""count"":1,""value"":[{""id"":""2"",""url"":""http://redstoneblock/DefaultCollection/_apis/wit/workItems/2""}]}")));
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/wit/workitems/2?api-version=5.0", "rumor")
                .Returns((HttpStatusCode.OK,
                    JObject.Parse(@"{""id"":2,""fields"":{""System.Title"": ""README has no useful content""}}")));

            var workItemLink = new AdoApiClient(store, httpJsonClient).GetBuildWorkItemLinks(
                    AdoBuildUrls.ParseBrowserUrl("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24"))
                .Single();

            Assert.AreEqual("2", workItemLink.Id);
            Assert.AreEqual("http://redstoneblock/DefaultCollection/Deployable/_workitems?_a=edit&id=2", workItemLink.LinkUrl);
            Assert.AreEqual("README has no useful content", workItemLink.Description);
        }

        [Test]
        public void PersonalAccessTokenIsOnlySentToItsOrigin()
        {
            var store = CreateSubstituteStore();
            var httpJsonClient = Substitute.For<IHttpJsonClient>();
            string passwordSent = ".";
            httpJsonClient.Get(null)
                .ReturnsForAnyArgs(ci =>
                {
                    passwordSent = ci.ArgAt<string>(1);
                    return (HttpStatusCode.OK, JObject.Parse(@"{""count"":0,""value"":[]}"));
                });

            // Request to other host should not include password
            new AdoApiClient(store, httpJsonClient)
                .GetBuildWorkItemsRefs(AdoBuildUrls.ParseBrowserUrl("http://someotherhost/DefaultCollection/Deployable/_build/results?buildId=24"));
            Assert.IsNull(passwordSent);

            // Request to origin should include password
            new AdoApiClient(store, httpJsonClient)
                .GetBuildWorkItemsRefs(AdoBuildUrls.ParseBrowserUrl("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24"));
            Assert.AreEqual("rumor", passwordSent);
        }

        [Test]
        public void AcceptsDeletedBuildAsPermanentEmptySet()
        {
            var store = CreateSubstituteStore();
            var httpJsonClient = Substitute.For<IHttpJsonClient>();
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/build/builds/7/workitems?api-version=5.0", "rumor")
                .Returns((HttpStatusCode.NotFound,
                    JObject.Parse(@"{""$id"":""1"",""message"":""The requested build 7 could not be found."",""errorCode"":0,""eventId"":3000}")));

            Assert.IsEmpty(new AdoApiClient(store, httpJsonClient).GetBuildWorkItemLinks(
                AdoBuildUrls.ParseBrowserUrl("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=7")));
        }

        [Test]
        public void AcceptsDeletedWorkItemAsPermanentMissingTitle()
        {
            var store = CreateSubstituteStore();
            var httpJsonClient = Substitute.For<IHttpJsonClient>();
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/build/builds/8/workitems?api-version=5.0", "rumor")
                .Returns((HttpStatusCode.OK,
                    JObject.Parse(@"{""count"":1,""value"":[{""id"":""999"",""url"":""http://redstoneblock/DefaultCollection/_apis/wit/workItems/999""}]}")));
            httpJsonClient.Get("http://redstoneblock/DefaultCollection/Deployable/_apis/wit/workitems/999?api-version=5.0", "rumor")
                .Returns((HttpStatusCode.NotFound,
                    JObject.Parse(@"{""$id"":""1"",""message"":""TF401232: Work item 999 does not exist."",""errorCode"":0,""eventId"":3200}")));

            var workItemLink = new AdoApiClient(store, httpJsonClient).GetBuildWorkItemLinks(
                    AdoBuildUrls.ParseBrowserUrl("http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=8"))
                .Single();

            Assert.AreEqual("999", workItemLink.Id);
            Assert.AreEqual("http://redstoneblock/DefaultCollection/Deployable/_workitems?_a=edit&id=999", workItemLink.LinkUrl);
            Assert.AreEqual("999", workItemLink.Description);
        }
    }
}