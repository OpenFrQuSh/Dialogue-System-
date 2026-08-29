using System.Collections.Generic;
using System.Reflection;
using DialogueSystem.Execution;
using DialogueSystem.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.Tests
{
    public sealed class DialogueHistoryPanelTests
    {
        [Test]
        public void SetHistory_RendersSpeakerLinesAndPlayerChoicesAsSeparatedEntries()
        {
            var panelObject = new GameObject("History", typeof(RectTransform));
            var panel = panelObject.AddComponent<DialogueHistoryPanel>();
            var history = new List<DialogueHistoryEntry>
            {
                new DialogueHistoryEntry(DialogueHistoryKind.Line, "Amiya", "Channel linked."),
                new DialogueHistoryEntry(DialogueHistoryKind.Choice, null, "Continue")
            };

            panel.SetHistory(history);

            Assert.That(panel.DisplayText, Does.Contain("Amiya\nChannel linked."));
            Assert.That(panel.DisplayText, Does.Contain("你的选择\nContinue"));
            Object.DestroyImmediate(panelObject);
        }

        [Test]
        public void ToggleVisible_WhenOpeningScrollsToLatestEntry()
        {
            var panelObject = new GameObject("History", typeof(RectTransform));
            var textObject = new GameObject("History Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(panelObject.transform, false);
            textObject.transform.SetParent(scrollObject.transform, false);
            var panel = panelObject.AddComponent<DialogueHistoryPanel>();
            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = textObject.GetComponent<RectTransform>();
            scrollRect.verticalNormalizedPosition = 1f;

            var configure = typeof(DialogueHistoryPanel).GetMethod(
                "Configure",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(configure, Is.Not.Null, "历史面板需要公开配置文本和滚动视图。 ");
            configure.Invoke(panel, new object[] { textObject.GetComponent<TMP_Text>(), scrollRect });
            panelObject.SetActive(false);

            panel.ToggleVisible();

            Assert.That(panelObject.activeSelf, Is.True);
            Assert.That(scrollRect.verticalNormalizedPosition, Is.EqualTo(0f));
            Object.DestroyImmediate(panelObject);
        }

        [TestCase(-10f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(20f, 0.5f)]
        [TestCase(40f, 1f)]
        [TestCase(100f, 1f)]
        [TestCase(180f, 0.5f)]
        [TestCase(200f, 0f)]
        [TestCase(210f, 0f)]
        public void EdgeFadeAlpha_TransitionsAtBothViewportBoundaries(float localY, float expected)
        {
            var faderType = typeof(DialogueHistoryPanel).Assembly.GetType(
                "DialogueSystem.UI.DialogueTextViewportFader");
            Assert.That(faderType, Is.Not.Null, "需要按字符位置计算上下边缘透明度的组件。 ");
            var evaluate = faderType.GetMethod(
                "EvaluateAlpha",
                BindingFlags.Static | BindingFlags.Public);
            Assert.That(evaluate, Is.Not.Null);

            var actual = (float)evaluate.Invoke(null, new object[] { localY, 0f, 200f, 40f });

            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }
    }
}
