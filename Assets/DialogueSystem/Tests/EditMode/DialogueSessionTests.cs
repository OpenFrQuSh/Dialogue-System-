using System.Linq;
using DialogueSystem.Data;
using DialogueSystem.Execution;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialogueSessionTests
    {
        [Test]
        public void SelectChoice_RecordsChoiceAndReachesConfiguredEnding()
        {
            var session = new DialogueSession();
            session.Start(DialogueTestAssetFactory.CreateBranchingAsset());

            Assert.That(session.Current.Kind, Is.EqualTo(DialogueNodeKind.Line));
            Assert.That(session.Current.Text, Is.EqualTo("通讯接入。"));

            session.Advance();
            Assert.That(session.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));

            session.SelectChoice(1);

            Assert.That(session.IsEnded, Is.True);
            Assert.That(session.EndingId, Is.EqualTo("decline"));
            Assert.That(session.History.Last().Kind, Is.EqualTo(DialogueHistoryKind.Choice));
            Assert.That(session.History.Last().Text, Is.EqualTo("拒绝"));
        }

        [Test]
        public void SelectChoice_RejectsIndexOutsideVisibleChoices()
        {
            var session = new DialogueSession();
            session.Start(DialogueTestAssetFactory.CreateBranchingAsset());
            session.Advance();

            Assert.That(
                () => session.SelectChoice(2),
                Throws.InvalidOperationException.With.Message.Contains("visible choice"));
        }
    }
}
