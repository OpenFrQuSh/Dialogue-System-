using System;
using System.Linq;
using DialogueSystem.Data;
using DialogueSystem.Execution;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialogueSkipTests
    {
        [Test]
        public void SkipToDecisionOrEnd_RecordsLinesAndStopsAtChoice()
        {
            var session = new DialogueSession();
            session.Start(DialogueTestAssetFactory.CreateSkipToChoiceAsset());

            var result = session.SkipToDecisionOrEnd();

            Assert.That(result, Is.EqualTo(DialogueSkipResult.ReachedChoice));
            Assert.That(session.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));
            Assert.That(session.History.Select(entry => entry.Text), Does.Contain("台词 B"));
        }

        [Test]
        public void SkipToDecisionOrEnd_RejectsCycleAtConfiguredLimit()
        {
            var asset = DialogueTestAssetFactory.CreateSkipToChoiceAsset();
            asset.Nodes[1].NextNodeId = "b";
            var session = new DialogueSession();
            session.Start(asset);

            Assert.That(
                () => session.SkipToDecisionOrEnd(3),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("b"));
        }
    }
}
