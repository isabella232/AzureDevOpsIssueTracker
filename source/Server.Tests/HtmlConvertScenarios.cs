using System;
using NSubstitute;
using NUnit.Framework;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.IssueTracker.AzureDevOps.WorkItems;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Tests
{
    [TestFixture]
    public class HtmlConvertScenarios
    {
        [Test]
        public void ConvertsEntitiesAndNewlines()
        {
            var plainText = new HtmlConvert(Substitute.For<ISystemLog>()).ToPlainText(@"one<br>two&nbsp;three<div>four</div>fi&lt;e<p>six</p>seven");

            Assert.AreEqual(string.Join(Environment.NewLine, @"one", "two\u00a0three", "four", "fi<e", "", "six", "", "seven"), plainText);
        }
    }
}