using System;
using DialogueSystem.Editor;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialoguePackagePathsTests
    {
        [TestCase("Assets/DialogueSystemGenerated", true)]
        [TestCase("Assets/DialogueSystemGenerated/Samples/Test.asset", true)]
        [TestCase("Assets/DialogueSystemGeneratedElsewhere/Test.asset", false)]
        [TestCase("Assets/UserContent/Test.asset", false)]
        [TestCase("Packages/com.zxxuh.dialogue-system/Fonts/NotoSansSC-Dynamic.asset", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsGeneratedAssetPath_OnlyAcceptsOwnedRoot(string path, bool expected)
        {
            Assert.That(DialoguePackagePaths.IsGeneratedAssetPath(path), Is.EqualTo(expected));
        }

        [Test]
        public void DeleteGeneratedAsset_RejectsUserContent()
        {
            Assert.That(
                () => DialoguePackagePaths.DeleteGeneratedAsset("Assets/UserContent/Test.asset"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("com.zxxuh.dialogue-system"));
        }
    }
}
