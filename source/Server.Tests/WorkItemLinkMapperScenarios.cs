using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Octopus.Data;
using Octopus.Data.Model;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Configuration;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;
using Octopus.Server.Extensibility.Results;
using Octopus.Server.MessageContracts.Features.BuildInformation;
using Octopus.Server.MessageContracts.Features.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
        private WorkItemLinkMapper CreateWorkItemLinkMapper(IAdoApiClient? client = null, IAzureDevOpsConfigurationStore? store = null, bool enabled = true)
        {
            var config = store ?? Substitute.For<IAzureDevOpsConfigurationStore>();
            config.GetIsEnabled().Returns(enabled);
            return new WorkItemLinkMapper(config, client ?? Substitute.For<IAdoApiClient>());
        }

        [Test]
        public void WhenDisabledReturnsExtensionIsDisabled()
        {
            var result = CreateWorkItemLinkMapper(enabled: false).Map(new OctopusBuildInformation
            {
                BuildUrl = "http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24"
            });
            Assert.IsInstanceOf<IFailureResultFromDisabledExtension>(result);
        }
        
        [Test]
        public void WhenNotFromAdoReturnsEmptyList()
        {
            ISuccessResult<WorkItemLink[]> result = (ISuccessResult<WorkItemLink[]>)CreateWorkItemLinkMapper().Map(new OctopusBuildInformation
            {
                BuildEnvironment = "Something",
                BuildUrl = "http://redstoneblock/DefaultCollection/Deployable/_build/results?buildId=24"
            });
            
            Assert.IsEmpty(result.Value);
        }
        
        [Test]
        public void WhenItFindsWorkItems()
        {
            var systemLog = Substitute.For<ISystemLog>();
            var store = Substitute.For<IAzureDevOpsConfigurationStore>();
            var jsonClient = Substitute.For<IHttpJsonClient>();
            var client = new AdoApiClient(systemLog, store, jsonClient,
                new HtmlConvert(systemLog));

            const string? baseUrl1 = "https://ado";
            const string? baseUrl2 = "https://anotherado";
            const string password1 = "password1";
            const string password2 = "password2";
            const int buildId = 16;
            const int workItemOneId = 24;
            const int workItemTwoId = 25;
            const string projectName = "IssueTracker";
            
            store.GetConnections().Returns(new List<AzureDevOpsConnection>
            {
                new()
                {
                    BaseUrl = baseUrl1, 
                    PersonalAccessToken = password1.ToSensitiveString(),
                    ReleaseNoteOptions = new ReleaseNoteOptions { ReleaseNotePrefix = "My note:" }
                },
                new()
                {
                    BaseUrl = baseUrl2, 
                    PersonalAccessToken = password2.ToSensitiveString()
                }
            });

            jsonClient.Get($"{baseUrl1}/{projectName}/_apis/build/builds/{buildId}/workitems?api-version=4.1", password1).Returns((HttpStatusCode.OK,
                JObject.Parse($@"
{{
    ""value"": [
        {{
            ""id"": ""{workItemOneId}"",
            ""url"": ""{baseUrl1}/{projectName}/_apis/wit/workItems/{workItemOneId}""
        }},
        {{
            ""id"": ""{workItemTwoId}"",
            ""url"": ""{baseUrl1}/{projectName}/_apis/wit/workItems/{workItemTwoId}""
        }}
    ]
}}
")));

            jsonClient.Get($"{baseUrl1}/{projectName}/_apis/wit/workitems/{workItemOneId}?api-version=4.1", password1).Returns((HttpStatusCode.OK,
                JObject.Parse(@"
{
    ""fields"": {
        ""System.CommentCount"": 3,
        ""System.Title"": ""This is the work item one title""
    }
}
")));
            jsonClient.Get($"{baseUrl1}/{projectName}/_apis/wit/workitems/{workItemOneId}/comments?api-version=4.1-preview.2", password1).Returns((HttpStatusCode.OK,
                JObject.Parse(@"
{
    ""totalCount"": 3,
    ""fromRevisionCount"": 0,
    ""count"": 3,
    ""comments"": [
        {
            ""text"": ""<div>my first comment</div>""
        },
        {
            ""text"": ""<div>My second comment</div>""
        },
        {
            ""text"": ""<div>My note: This one comment from a comment with th special prefix </div>""
        }
    ]
}
")));
            
            jsonClient.Get($"{baseUrl1}/{projectName}/_apis/wit/workitems/{workItemTwoId}?api-version=4.1", password1).Returns((HttpStatusCode.OK,
                JObject.Parse(@"
{
    ""fields"": {
        ""System.CommentCount"": 1,
        ""System.Title"": ""This is the work item two title""
    }
}
")));
            jsonClient.Get($"{baseUrl1}/{projectName}/_apis/wit/workitems/{workItemTwoId}/comments?api-version=4.1-preview.2", password1).Returns((HttpStatusCode.OK,
                JObject.Parse(@"
{
    ""totalCount"": 1,
    ""fromRevisionCount"": 0,
    ""count"": 1,
    ""comments"": [
        {
            ""text"": ""<div>Not a special comment</div>""
        }
    ]
}
")));
            
            ISuccessResult<WorkItemLink[]> result = (ISuccessResult<WorkItemLink[]>)CreateWorkItemLinkMapper(client, store).Map(new OctopusBuildInformation
            {
                BuildEnvironment = "Azure DevOps",
                BuildUrl = $"{baseUrl1}/{projectName}/_build/results?buildId={buildId}"
            });

            var workItemLinks = result.Value;
            Assert.AreEqual(2, workItemLinks.Length);
            Assert.AreEqual("This one comment from a comment with th special prefix", workItemLinks[0].Description);
            Assert.AreEqual("This is the work item two title", workItemLinks[1].Description);
        }
    }
}