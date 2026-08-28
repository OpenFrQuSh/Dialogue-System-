using System.Collections.Generic;
using DialogueSystem.Data;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialogueConditionTests
    {
        [Test]
        public void BoolCondition_MatchesExpectedValue()
        {
            var values = new Dictionary<string, DialogueValue>
            {
                ["trusted"] = DialogueValue.FromBool(true)
            };
            var condition = DialogueTestAssetFactory.BoolCondition("trusted", true);

            Assert.That(condition.IsMet(values), Is.True);
        }

        [TestCase(DialogueComparison.Equal, 3, 3, true)]
        [TestCase(DialogueComparison.Greater, 4, 3, true)]
        [TestCase(DialogueComparison.LessOrEqual, 4, 3, false)]
        public void IntCondition_UsesConfiguredComparison(
            DialogueComparison comparison,
            int actual,
            int expected,
            bool result)
        {
            var values = new Dictionary<string, DialogueValue>
            {
                ["score"] = DialogueValue.FromInt(actual)
            };
            var condition = DialogueTestAssetFactory.IntCondition("score", comparison, expected);

            Assert.That(condition.IsMet(values), Is.EqualTo(result));
        }

        [Test]
        public void AddIntEffect_ChangesExistingValue()
        {
            IDictionary<string, DialogueValue> values = new Dictionary<string, DialogueValue>
            {
                ["score"] = DialogueValue.FromInt(2)
            };

            DialogueTestAssetFactory.AddIntEffect("score", 3).Apply(values);

            Assert.That(values["score"].IntValue, Is.EqualTo(5));
        }
    }
}
