using System;
using NSubstitute;
using NUnit.Framework;
using Octopus.Diagnostics;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Tests
{
    [TestFixture]
    public class HtmlConvertScenarios
    {
        [Test]
        public void ConvertsEntitiesAndNewlines()
        {
            var htmlConvert = new WorkItems.HtmlConvert(Substitute.For<ILog>());

            Assert.AreEqual(string.Join(Environment.NewLine, @"one", "two\u00a0three", "four", "fi<e", "", "six", "", "seven"),
                htmlConvert.ToPlainText(@"one<br>two&nbsp;three<div>four</div>fi&lt;e<p>six</p>seven"));
        }
    }
}