using System.Collections.Generic;
using System.Reflection;
using DialogueSystem.Data;
using DialogueSystem.Execution;
using DialogueSystem.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DialogueSystem.Editor
{
    internal static class DialogueSampleBuilder
    {
        [MenuItem("Tools/Dialogue System/Create Sample Scene")]
        private static void CreateSampleScene()
        {
            var scenePath = DialoguePackagePaths.GeneratedSamplesRoot + "/DialogueSystemSample.unity";
            var assetPath = DialoguePackagePaths.GeneratedSamplesRoot + "/DialogueSystemSample.asset";
            var prefabPath = DialoguePackagePaths.GeneratedSamplesRoot + "/DialogueSystemCanvas.prefab";
            DialoguePackagePaths.DeleteGeneratedAsset(scenePath);
            DialoguePackagePaths.DeleteGeneratedAsset(assetPath);
            DialoguePackagePaths.DeleteGeneratedAsset(prefabPath);
            DialoguePackagePaths.EnsureGeneratedFolder(DialoguePackagePaths.GeneratedSamplesRoot);

            var dialogue = BuildDialogueAsset(assetPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sampleRoot = new GameObject("Dialogue System Demo");
            // 即使 Canvas 使用 Overlay，保留主相机也能让 Game 视图正常呈现并作为后续背景扩展入口。
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var mainCamera = cameraObject.GetComponent<Camera>();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.008f, 0.012f, 0.02f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = new GameObject("Dialogue Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            canvas.transform.SetParent(sampleRoot.transform, false);

            var root = canvas.transform;
            var runnerObject = new GameObject("Dialogue Runner");
            runnerObject.transform.SetParent(sampleRoot.transform, false);
            var runner = runnerObject.AddComponent<DialogueRunner>();
            var view = runnerObject.AddComponent<DialogueView>();
            var bootstrap = runnerObject.AddComponent<DialogueDemoBootstrap>();

            var historyButton = CreateButton(root, "History", "HISTORY", new Vector2(150f, 52f));
            SetAnchors(historyButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -38f));
            var speedButton = CreateButton(root, "Speed", "1X", new Vector2(110f, 52f));
            SetAnchors(speedButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-400f, -38f));
            var autoButton = CreateButton(root, "Auto", "AUTO", new Vector2(150f, 52f));
            SetAnchors(autoButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -38f));
            var skipButton = CreateButton(root, "Skip", "SKIP", new Vector2(110f, 52f));
            SetAnchors(skipButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-90f, -38f));

            var dialoguePanel = CreatePanel(root, "Dialogue Panel", new Color(0.015f, 0.02f, 0.03f, 0.94f));
            var panelRect = dialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f); panelRect.anchorMax = new Vector2(1f, 0f); panelRect.offsetMin = new Vector2(0f, 0f); panelRect.offsetMax = new Vector2(0f, 235f);
            var speaker = CreateText(dialoguePanel.transform, "Speaker", "OPERATOR", 34, TextAlignmentOptions.Left);
            SetAnchors(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -48f), new Vector2(320f, 48f));
            var body = CreateText(dialoguePanel.transform, "Body", "", 38, TextAlignmentOptions.Left);
            body.enableWordWrapping = true;
            SetStretchOffsets(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(310f, 35f), new Vector2(-70f, -50f));
            var dialogueButton = dialoguePanel.AddComponent<Button>();

            var choicesRoot = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            choicesRoot.transform.SetParent(root, false);
            var choicesRect = choicesRoot.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0.5f, 0f); choicesRect.anchorMax = new Vector2(0.5f, 0f); choicesRect.anchoredPosition = new Vector2(0f, 280f); choicesRect.sizeDelta = new Vector2(740f, 0f);
            choicesRoot.GetComponent<VerticalLayoutGroup>().spacing = 14f;
            choicesRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var choicePanel = choicesRoot.AddComponent<DialogueChoiceListPanel>();
            var choicePrefab = CreateButton(choicesRoot.transform, "Choice Button Template", "CHOICE", new Vector2(740f, 64f));
            choicePrefab.gameObject.SetActive(false);
            choicePanel.Configure(choicesRoot.transform, choicePrefab);

            // 普通 Sample 与导览 Demo 共用完整历史面板，避免两套生成结果在功能和中文显示上再次分叉。
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                DialoguePackagePaths.BundledFontAssetPath);
            // 包内字体是示例中文显示的必需资源；尽早失败能给出明确的重装指引。
            if (chineseFont == null)
            {
                throw new System.InvalidOperationException(
                    $"[{DialoguePackagePaths.PackageId}] 未找到随包中文字体 "
                    + $"{DialoguePackagePaths.BundledFontAssetPath}，请重新安装插件。 ");
            }

            var historyPanel = DialogueHistoryUiFactory.Create(
                root,
                new Vector2(0.1f, 0.15f),
                new Vector2(0.9f, 0.85f),
                chineseFont);

            SetPrivate(view, "speakerText", speaker);
            SetPrivate(view, "bodyText", body);
            view.ConfigureControlLabels(speedButton.GetComponentInChildren<TMP_Text>(), autoButton.GetComponentInChildren<TMP_Text>());
            view.BindChoicePanel(choicePanel);
            view.BindHistoryPanel(historyPanel);
            UnityEventTools.AddPersistentListener(speedButton.onClick, view.HandleSpeedClick);
            UnityEventTools.AddPersistentListener(autoButton.onClick, view.HandleAutoClick);
            UnityEventTools.AddPersistentListener(skipButton.onClick, view.HandleSkipClick);
            UnityEventTools.AddPersistentListener(dialogueButton.onClick, view.HandleAdvanceClick);
            UnityEventTools.AddPersistentListener(historyButton.onClick, historyPanel.ToggleVisible);
            SetPrivate(bootstrap, "dialogueRunner", runner);
            SetPrivate(bootstrap, "dialogueView", view);
            SetPrivate(bootstrap, "dialogueAsset", dialogue);
            PrefabUtility.SaveAsPrefabAsset(sampleRoot, prefabPath);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        private static DialogueAsset BuildDialogueAsset(string path)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            SetPrivate(asset, "entryNodeId", "intro");
            SetPrivate(asset, "nodes", new List<DialogueNodeData>
            {
                new DialogueNodeData { Id = "intro", Kind = DialogueNodeKind.Line, Speaker = "OPERATOR", Text = "A restricted channel has been established.", NextNodeId = "decision" },
                new DialogueNodeData { Id = "decision", Kind = DialogueNodeKind.Choice, Text = "", Choices = new List<DialogueChoiceData>
                    { new DialogueChoiceData { Text = "Proceed with the operation", NextNodeId = "proceed" }, new DialogueChoiceData { Text = "Abort and withdraw", NextNodeId = "withdraw" } } },
                new DialogueNodeData { Id = "proceed", Kind = DialogueNodeKind.Line, Speaker = "OPERATOR", Text = "Acknowledged. Continue under radio silence.", NextNodeId = "success" },
                new DialogueNodeData { Id = "withdraw", Kind = DialogueNodeKind.Line, Speaker = "OPERATOR", Text = "Withdrawal confirmed. The channel will be erased.", NextNodeId = "retreat" },
                new DialogueNodeData { Id = "success", Kind = DialogueNodeKind.End, EndingId = "operation_complete", EndingDescription = "Operation complete." },
                new DialogueNodeData { Id = "retreat", Kind = DialogueNodeKind.End, EndingId = "withdrawn", EndingDescription = "Operation withdrawn." }
            });
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false); panel.GetComponent<Image>().color = color; return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            var buttonObject = CreatePanel(parent, name, new Color(0.08f, 0.1f, 0.14f, 0.96f)); var button = buttonObject.AddComponent<Button>(); button.GetComponent<RectTransform>().sizeDelta = size;
            var text = CreateText(buttonObject.transform, "Label", label, 24, TextAlignmentOptions.Center); SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(parent, false); var text = textObject.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.alignment = alignment; text.color = new Color(0.9f, 0.94f, 1f); return text;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size = default)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            // 固定在边缘的元素以对应边缘为 Pivot，避免中心 Pivot 把控件推到屏幕外。
            rect.pivot = new Vector2(
                Mathf.Approximately(min.x, max.x) ? min.x : 0.5f,
                Mathf.Approximately(min.y, max.y) ? min.y : 0.5f);
            rect.anchoredPosition = position;
            if (size != default) rect.sizeDelta = size;
        }

        private static void SetStretchOffsets(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetPrivate(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
