using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DialogueSystem.Data;
using DialogueSystem.Execution;
using DialogueSystem.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DialogueSystem.Tests
{
    public sealed class DialogueTourControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator EndingStep_MovesOnceAndStartsNextDialogue()
        {
            var rig = CreateTourRig(2, 0.01f);
            rig.Controller.BeginTour();
            Assert.That(rig.Controller.CurrentStepIndex, Is.Zero);

            rig.Runner.Skip();
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(rig.Controller.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(rig.Controller.State, Is.EqualTo(DialogueTourState.Presenting));
            Assert.That(rig.Runner.Current.Text, Is.EqualTo("Step 2"));
            rig.Dispose();
        }

        [Test]
        public void BeginTour_BindsTheViewBeforeFirstPresentation()
        {
            var rig = CreateTourRig(1, 0f);

            rig.Controller.BeginTour();

            Assert.That(rig.BodyText.text, Is.EqualTo("Step 1"));
            rig.Dispose();
        }

        [UnityTest]
        public IEnumerator EndingFinalStep_DisablesTheDialogueCanvas()
        {
            var rig = CreateTourRig(1, 0.01f);
            rig.Controller.BeginTour();

            rig.Runner.Skip();
            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(rig.Controller.State, Is.EqualTo(DialogueTourState.Completed));
            Assert.That(rig.CanvasRoot.activeSelf, Is.False);
            rig.Dispose();
        }

        [UnityTest]
        public IEnumerator EndingAgainDuringTransition_DoesNotSkipAnotherStep()
        {
            var rig = CreateTourRig(3, 0.02f);
            rig.Controller.BeginTour();

            rig.Runner.Skip();
            Assert.That(rig.Controller.State, Is.EqualTo(DialogueTourState.Transitioning));

            var duplicateAsset = CreateLineAsset("Duplicate ending event");
            rig.Runner.StartDialogue(duplicateAsset);
            rig.Runner.Skip();
            yield return new WaitForSecondsRealtime(0.12f);

            Assert.That(rig.Controller.CurrentStepIndex, Is.EqualTo(1));
            Assert.That(rig.Runner.Current.Text, Is.EqualTo("Step 2"));
            Object.DestroyImmediate(duplicateAsset);
            rig.Dispose();
        }

        [Test]
        public void BeginTour_WithNoStepsEntersFailedState()
        {
            var rig = CreateTourRig(0, 0f);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("没有可播放的步骤"));

            rig.Controller.BeginTour();

            Assert.That(rig.Controller.State, Is.EqualTo(DialogueTourState.Failed));
            rig.Dispose();
        }

        private static TourRig CreateTourRig(int stepCount, float transitionSeconds)
        {
            var root = new GameObject("Tour Test Rig");
            var cameraObject = new GameObject("Camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            var runtimeObject = new GameObject("Runtime");
            runtimeObject.transform.SetParent(root.transform, false);
            var canvasRoot = new GameObject("Canvas", typeof(Canvas), typeof(CanvasGroup));
            canvasRoot.transform.SetParent(root.transform, false);
            var bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(canvasRoot.transform, false);
            var bodyText = bodyObject.AddComponent<TextMeshProUGUI>();

            var points = new List<Transform>();
            for (var index = 0; index < 4; index++)
            {
                var point = new GameObject("Point " + index).transform;
                point.SetParent(root.transform, false);
                point.position = new Vector3(index * 2f, index * 0.25f, 0f);
                point.rotation = Quaternion.Euler(0f, index * 10f, 0f);
                points.Add(point);
            }

            var runner = runtimeObject.AddComponent<DialogueRunner>();
            var view = runtimeObject.AddComponent<DialogueView>();
            typeof(DialogueView)
                .GetField("bodyText", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, bodyText);
            var spline = runtimeObject.AddComponent<DialogueCameraSpline>();
            spline.Configure(cameraObject.GetComponent<Camera>(), points);
            var fader = canvasRoot.AddComponent<DialogueCanvasFader>();
            fader.Configure(canvasRoot.GetComponent<CanvasGroup>(), transitionSeconds);
            var controller = runtimeObject.AddComponent<DialogueTourController>();
            var steps = new List<DialogueTourStep>();
            var assets = new List<DialogueAsset>();
            for (var index = 0; index < stepCount; index++)
            {
                var asset = CreateLineAsset("Step " + (index + 1));
                assets.Add(asset);
                steps.Add(new DialogueTourStep(asset, index, transitionSeconds, 0f));
            }

            controller.Configure(runner, view, spline, fader, steps);
            return new TourRig(root, canvasRoot, bodyText, runner, controller, assets);
        }

        private static DialogueAsset CreateLineAsset(string text)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            var nodes = new List<DialogueNodeData>
            {
                new DialogueNodeData
                {
                    Id = "line",
                    Kind = DialogueNodeKind.Line,
                    Speaker = "Narrator",
                    Text = text,
                    NextNodeId = "end"
                },
                new DialogueNodeData { Id = "end", Kind = DialogueNodeKind.End, EndingId = "complete" }
            };

            // 测试使用内存资产，直接注入真实序列化字段以覆盖 Runner 的完整运行路径。
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(DialogueAsset).GetField("entryNodeId", Flags).SetValue(asset, "line");
            typeof(DialogueAsset).GetField("nodes", Flags).SetValue(asset, nodes);
            return asset;
        }

        private sealed class TourRig
        {
            private readonly GameObject root;
            private readonly IReadOnlyList<DialogueAsset> assets;

            public TourRig(
                GameObject root,
                GameObject canvasRoot,
                TMP_Text bodyText,
                DialogueRunner runner,
                DialogueTourController controller,
                IReadOnlyList<DialogueAsset> assets)
            {
                this.root = root;
                this.assets = assets;
                CanvasRoot = canvasRoot;
                BodyText = bodyText;
                Runner = runner;
                Controller = controller;
            }

            public GameObject CanvasRoot { get; }

            public TMP_Text BodyText { get; }

            public DialogueRunner Runner { get; }

            public DialogueTourController Controller { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(root);
                foreach (var asset in assets)
                {
                    Object.DestroyImmediate(asset);
                }
            }
        }
    }
}
