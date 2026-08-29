using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using DialogueSystem.Data;
using DialogueSystem.Execution;
using DialogueSystem.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DialogueSystem.Tests
{
    public sealed class DialogueViewPlayModeTests
    {
        [Test]
        public void SpeedClick_CyclesOneTwoFour()
        {
            var viewObject = new GameObject("View");
            var view = viewObject.AddComponent<DialogueView>();

            Assert.That(view.PlaybackSpeed, Is.EqualTo(1f));
            view.HandleSpeedClick();
            Assert.That(view.PlaybackSpeed, Is.EqualTo(2f));
            view.HandleSpeedClick();
            Assert.That(view.PlaybackSpeed, Is.EqualTo(4f));
            view.HandleSpeedClick();
            Assert.That(view.PlaybackSpeed, Is.EqualTo(1f));

            Object.DestroyImmediate(viewObject);
        }

        [Test]
        public void ConfigureControlLabels_ReflectsSpeedAndAutoState()
        {
            var viewObject = new GameObject("View");
            var speedObject = new GameObject("Speed");
            var autoObject = new GameObject("Auto");
            var view = viewObject.AddComponent<DialogueView>();
            var speedLabel = speedObject.AddComponent<TextMeshProUGUI>();
            var autoLabel = autoObject.AddComponent<TextMeshProUGUI>();

            view.ConfigureControlLabels(speedLabel, autoLabel);
            Assert.That(speedLabel.text, Is.EqualTo("1X"));
            Assert.That(autoLabel.text, Is.EqualTo("AUTO"));
            view.HandleSpeedClick();
            view.HandleAutoClick();
            Assert.That(speedLabel.text, Is.EqualTo("2X"));
            Assert.That(autoLabel.text, Is.EqualTo("AUTO ON"));

            Object.DestroyImmediate(autoObject);
            Object.DestroyImmediate(speedObject);
            Object.DestroyImmediate(viewObject);
        }

        [Test]
        public void AutoClick_TogglesAutoAdvance()
        {
            var viewObject = new GameObject("View");
            var view = viewObject.AddComponent<DialogueView>();

            Assert.That(view.IsAutoAdvanceEnabled, Is.False);
            view.HandleAutoClick();
            Assert.That(view.IsAutoAdvanceEnabled, Is.True);
            view.HandleAutoClick();
            Assert.That(view.IsAutoAdvanceEnabled, Is.False);

            Object.DestroyImmediate(viewObject);
        }

        [UnityTest]
        public IEnumerator AutoAdvance_MovesToTheNextDecisionAfterTheLineFinishes()
        {
            var runnerObject = new GameObject("Runner");
            var viewObject = new GameObject("View");
            var canvasObject = new GameObject("Canvas", typeof(Canvas));
            var bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(canvasObject.transform, false);
            var runner = runnerObject.AddComponent<DialogueRunner>();
            var view = viewObject.AddComponent<DialogueView>();
            SetPrivateField(view, "bodyText", bodyObject.AddComponent<TextMeshProUGUI>());

            view.Bind(runner);
            view.HandleAutoClick();
            runner.StartDialogue(CreateBranchingAsset());
            yield return null;

            view.Tick(10f);
            view.Tick(10f);
            Assert.That(runner.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));

            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(runnerObject);
        }

        [Test]
        public void SkipClick_MovesToTheNextDecision()
        {
            var runnerObject = new GameObject("Runner");
            var viewObject = new GameObject("View");
            var runner = runnerObject.AddComponent<DialogueRunner>();
            var view = viewObject.AddComponent<DialogueView>();

            view.Bind(runner);
            runner.StartDialogue(CreateBranchingAsset());
            view.HandleSkipClick();
            Assert.That(runner.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));

            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(runnerObject);
        }

        [Test]
        public void BindHistoryPanel_ReceivesPresentedDialogueHistory()
        {
            var runnerObject = new GameObject("Runner");
            var viewObject = new GameObject("View");
            var panelObject = new GameObject("History");
            var runner = runnerObject.AddComponent<DialogueRunner>();
            var view = viewObject.AddComponent<DialogueView>();
            var panel = panelObject.AddComponent<DialogueHistoryPanel>();

            view.Bind(runner);
            view.BindHistoryPanel(panel);
            runner.StartDialogue(CreateBranchingAsset());

            Assert.That(panel.DisplayText, Does.Contain("Channel linked."));
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(runnerObject);
        }

        [Test]
        public void BindChoicePanel_PresentsVisibleChoicesAndSelectsTheirIndex()
        {
            var runnerObject = new GameObject("Runner");
            var viewObject = new GameObject("View");
            var panelObject = new GameObject("Choices", typeof(RectTransform));
            var contentObject = new GameObject("Content", typeof(RectTransform));
            var prefabObject = new GameObject("Choice Prefab", typeof(RectTransform), typeof(Image), typeof(Button));
            contentObject.transform.SetParent(panelObject.transform, false);
            var runner = runnerObject.AddComponent<DialogueRunner>();
            var view = viewObject.AddComponent<DialogueView>();
            var panel = panelObject.AddComponent<DialogueChoiceListPanel>();
            panel.Configure(contentObject.transform, prefabObject.GetComponent<Button>());

            view.Bind(runner);
            view.BindChoicePanel(panel);
            runner.StartDialogue(CreateBranchingAsset());
            runner.Advance();
            Assert.That(panel.Buttons.Count, Is.EqualTo(1));

            panel.Buttons[0].onClick.Invoke();
            Assert.That(runner.Current.Kind, Is.EqualTo(DialogueNodeKind.End));

            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(runnerObject);
        }

        [UnityTest]
        public IEnumerator AdvanceClick_CompletesLineBeforeAdvancingToChoice()
        {
            var runnerObject = new GameObject("Runner");
            var viewObject = new GameObject("View");
            // 使用真实 Canvas 层级，确保 TMP 在测试中也会构建字符网格。
            var canvasObject = new GameObject("Canvas", typeof(Canvas));
            var bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(canvasObject.transform, false);
            var runner = runnerObject.AddComponent<DialogueRunner>();
            var view = viewObject.AddComponent<DialogueView>();
            var bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
            SetPrivateField(view, "bodyText", bodyText);

            view.Bind(runner);
            runner.StartDialogue(CreateBranchingAsset());

            // 让 TMP 完成一个 UGUI 帧，测试与实际 Canvas 的网格生成时机保持一致。
            yield return null;

            Assert.That(bodyText.text, Is.EqualTo("Channel linked."));
            Assert.That(bodyText.textInfo, Is.Not.Null, "TMP must finish its UI lifecycle before character assertions.");
            Assert.That(bodyText.maxVisibleCharacters, Is.LessThan(bodyText.textInfo.characterCount));

            view.HandleAdvanceClick();
            Assert.That(bodyText.maxVisibleCharacters, Is.EqualTo(bodyText.textInfo.characterCount));

            view.HandleAdvanceClick();
            Assert.That(runner.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));

            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(runnerObject);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(DialogueView)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static DialogueAsset CreateBranchingAsset()
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            var nodes = new List<DialogueNodeData>
            {
                new DialogueNodeData { Id = "line", Kind = DialogueNodeKind.Line, Text = "Channel linked.", NextNodeId = "choice" },
                new DialogueNodeData { Id = "choice", Kind = DialogueNodeKind.Choice, Choices = new List<DialogueChoiceData> { new DialogueChoiceData { Text = "Continue", NextNodeId = "end" } } },
                new DialogueNodeData { Id = "end", Kind = DialogueNodeKind.End, EndingId = "end" }
            };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(DialogueAsset).GetField("entryNodeId", flags).SetValue(asset, "line");
            typeof(DialogueAsset).GetField("nodes", flags).SetValue(asset, nodes);
            return asset;
        }
    }
}
