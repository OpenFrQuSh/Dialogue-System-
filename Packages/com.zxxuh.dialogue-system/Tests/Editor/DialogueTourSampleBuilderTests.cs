using System.Linq;
using DialogueSystem.Data;
using DialogueSystem.Editor;
using DialogueSystem.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.Tests
{
    public sealed class DialogueTourSampleBuilderTests
    {
        [Test]
        public void BuildAll_CreatesThreeCompleteSampleFoldersWithoutRemovingUserContent()
        {
            var sentinelPath = DialoguePackagePaths.GeneratedRoot + "/UserContentSentinel.asset";
            DialoguePackagePaths.EnsureGeneratedFolder(DialoguePackagePaths.GeneratedRoot);
            DialoguePackagePaths.DeleteGeneratedAsset(sentinelPath);
            // 哨兵模拟生成目录中的用户内容，验证生成器只覆盖自己拥有的固定文件。
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DialogueAsset>(), sentinelPath);

            try
            {
                DialogueTourSampleBuilder.BuildAll();

                AssertSampleExists("01_AncientCityTour", "AncientCityTour");
                AssertSampleExists("02_AbandonedLabTour", "AbandonedLabTour");
                AssertSampleExists("03_RainyStreetTour", "RainyStreetTour");
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<DialogueAsset>(sentinelPath),
                    Is.Not.Null,
                    "生成器不得删除生成目录中的主人自定义内容。");
            }
            finally
            {
                // 测试只清理由本测试创建且通过路径契约验证的固定哨兵资源。
                DialoguePackagePaths.DeleteGeneratedAsset(sentinelPath);
            }
        }

        [Test]
        public void BuildAll_CreatesScrollableStoryHistoryWithEdgeFade()
        {
            DialogueTourSampleBuilder.BuildAll();
            EditorSceneManager.OpenScene(
                DialoguePackagePaths.GeneratedSamplesRoot
                + "/01_AncientCityTour/AncientCityTour.unity");

            var title = FindSceneComponent<TMP_Text>("Story Title");
            var scrollRect = FindSceneComponent<ScrollRect>("History Scroll View");
            var historyText = FindSceneComponent<TMP_Text>("History Text");

            Assert.That(title, Is.Not.Null);
            Assert.That(title.text, Is.EqualTo("故事情节"));
            Assert.That(scrollRect, Is.Not.Null);
            Assert.That(scrollRect.vertical, Is.True);
            Assert.That(historyText, Is.Not.Null);
            Assert.That(
                historyText.GetComponent("DialogueTextViewportFader"),
                Is.Not.Null,
                "上下边界必须拥有文字透明度渐变。 ");
        }

        [Test]
        public void BuildAll_PersistsDialogueViewHistoryBindingAfterSceneReload()
        {
            DialogueTourSampleBuilder.BuildAll();
            EditorSceneManager.OpenScene(
                DialoguePackagePaths.GeneratedSamplesRoot
                + "/01_AncientCityTour/AncientCityTour.unity");
            var view = Resources.FindObjectsOfTypeAll<DialogueView>()
                .First(component => component.gameObject.scene.IsValid());
            var serializedView = new SerializedObject(view);
            var historyPanelProperty = serializedView.FindProperty("historyPanel");

            Assert.That(
                historyPanelProperty,
                Is.Not.Null,
                "历史面板引用必须能随场景保存。 ");
            Assert.That(historyPanelProperty.objectReferenceValue, Is.Not.Null);
        }

        private static T FindSceneComponent<T>(string objectName) where T : Component
        {
            // 历史面板默认隐藏，测试必须从已加载场景中包含 inactive 对象进行查找。
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(component =>
                    component.gameObject.scene.IsValid()
                    && component.gameObject.name == objectName);
        }

        private static void AssertSampleExists(string folderName, string sceneName)
        {
            var root = DialoguePackagePaths.GeneratedSamplesRoot + "/" + folderName;
            Assert.That(AssetDatabase.IsValidFolder(root), Is.True, root);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>($"{root}/{sceneName}.unity"),
                Is.Not.Null);

            for (var index = 1; index <= 3; index++)
            {
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<DialogueAsset>($"{root}/Step0{index}.asset"),
                    Is.Not.Null);
            }
        }
    }
}
