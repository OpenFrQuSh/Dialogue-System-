using DialogueSystem.UI;
using DialogueSystem.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;

namespace DialogueSystem.Tests
{
    public sealed class DialogueChineseFontProviderTests
    {
        [Test]
        public void SelectInstalledFont_PrefersMicrosoftYaHei()
        {
            var installed = new[] { "SimSun", "Microsoft YaHei", "Noto Sans SC" };

            Assert.That(
                DialogueChineseFontProvider.SelectInstalledFont(installed),
                Is.EqualTo("Microsoft YaHei"));
        }

        [Test]
        public void SelectInstalledFont_WhenNoCandidateReturnsNull()
        {
            Assert.That(
                DialogueChineseFontProvider.SelectInstalledFont(new[] { "Liberation Sans" }),
                Is.Null);
        }

        [Test]
        public void SelectInstalledFont_IsCaseInsensitive()
        {
            Assert.That(
                DialogueChineseFontProvider.SelectInstalledFont(new[] { "microsoft yahei ui" }),
                Is.EqualTo("microsoft yahei ui"));
        }

        [Test]
        public void SelectInstalledFont_UsesStableFallbackPriority()
        {
            var installed = new[] { "Noto Sans SC", "SimHei", "SimSun" };

            Assert.That(DialogueChineseFontProvider.SelectInstalledFont(installed), Is.EqualTo("SimHei"));
        }

        [Test]
        public void BundledFontAsset_ContainsRepresentativeChineseGlyphs()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                DialoguePackagePaths.BundledFontAssetPath);

            Assert.That(font, Is.Not.Null, "样例必须附带可随项目发布的中文 TMP 字体资产。");
            Assert.That(font.HasCharacters("中文对话步骤历史选择"), Is.True);
        }
    }
}
