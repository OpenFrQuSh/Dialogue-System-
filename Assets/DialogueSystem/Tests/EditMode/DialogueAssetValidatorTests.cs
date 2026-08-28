using DialogueSystem.Editor;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialogueAssetValidatorTests
    {
        [Test]
        public void Validate_NullAsset_ReportsStableEmptyAssetCode()
        {
            var issues = DialogueAssetValidator.Validate(null);

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo("DIALOGUE_EMPTY_ASSET"));
        }
    }
}
