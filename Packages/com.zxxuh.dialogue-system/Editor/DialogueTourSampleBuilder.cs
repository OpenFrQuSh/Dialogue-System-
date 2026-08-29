using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
    public static class DialogueTourSampleBuilder
    {
        private const string SamplesRoot = DialoguePackagePaths.GeneratedSamplesRoot;

        [MenuItem("Tools/Dialogue System/Create Guided Tour Samples")]
        public static void BuildAll()
        {
            DialoguePackagePaths.EnsureGeneratedFolder(SamplesRoot);
            var chineseFont = LoadBundledChineseFontAsset();
            var generatedScenes = new List<string>();

            foreach (var definition in CreateDefinitions())
            {
                generatedScenes.Add(BuildDemo(definition, chineseFont));
            }

            AddScenesToBuildSettings(generatedScenes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (generatedScenes.Count > 0)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(generatedScenes[0]);
            }

            Debug.Log("[DialogueSystem] 已生成三套分步骤中文导览 Demo。", Selection.activeObject);
        }

        private static string BuildDemo(DemoDefinition definition, TMP_FontAsset chineseFont)
        {
            var folder = $"{SamplesRoot}/{definition.FolderName}";
            DialoguePackagePaths.EnsureGeneratedFolder(folder);
            var scenePath = $"{folder}/{definition.SceneName}.unity";

            // 只覆盖生成器拥有的固定文件，避免误删 Samples 下的主人自定义内容。
            DialoguePackagePaths.DeleteGeneratedAsset(scenePath);
            var dialogueAssets = new List<DialogueAsset>();
            for (var index = 0; index < 3; index++)
            {
                var assetPath = $"{folder}/Step0{index + 1}.asset";
                DialoguePackagePaths.DeleteGeneratedAsset(assetPath);
                dialogueAssets.Add(BuildDialogueAsset(assetPath, definition, index));
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var demoRoot = new GameObject(definition.SceneName + " Demo");
            var camera = CreateCamera(definition.BackgroundColor);
            CreateDirectionalLight();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var pathRoot = new GameObject("Camera Path").transform;
            pathRoot.SetParent(demoRoot.transform, false);
            var pathPoints = CreatePathPoints(pathRoot);
            camera.transform.SetPositionAndRotation(pathPoints[0].position, pathPoints[0].rotation);

            var canvas = CreateCanvas(demoRoot.transform);
            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            var fader = canvas.AddComponent<DialogueCanvasFader>();
            fader.Configure(canvasGroup, 0.2f);
            var fontProvider = canvas.AddComponent<DialogueChineseFontProvider>();
            fontProvider.Configure(canvas.transform, chineseFont);

            var runtimeObject = new GameObject("Dialogue Runtime");
            runtimeObject.transform.SetParent(demoRoot.transform, false);
            var runner = runtimeObject.AddComponent<DialogueRunner>();
            var view = runtimeObject.AddComponent<DialogueView>();
            ConfigureDialogueUi(canvas.transform, view, chineseFont);

            var spline = runtimeObject.AddComponent<DialogueCameraSpline>();
            spline.Configure(camera, pathPoints);
            var tour = runtimeObject.AddComponent<DialogueTourController>();
            var steps = new List<DialogueTourStep>
            {
                new DialogueTourStep(dialogueAssets[0], 0, 0f, 0f),
                new DialogueTourStep(dialogueAssets[1], 1, 0.8f, 0.1f),
                new DialogueTourStep(dialogueAssets[2], 2, 0.8f, 0.1f)
            };
            tour.Configure(runner, view, spline, fader, steps, true);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            return scenePath;
        }

        private static DialogueAsset BuildDialogueAsset(
            string path,
            DemoDefinition definition,
            int stepIndex)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.name = $"{definition.SceneName} Step {stepIndex + 1}";
            var nodes = definition.IsBranching
                ? CreateBranchingNodes(definition, stepIndex)
                : CreateLinearNodes(definition, stepIndex);
            SetPrivate(asset, "entryNodeId", "line_1");
            SetPrivate(asset, "nodes", nodes);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static List<DialogueNodeData> CreateLinearNodes(
            DemoDefinition definition,
            int stepIndex)
        {
            return new List<DialogueNodeData>
            {
                new DialogueNodeData
                {
                    Id = "line_1",
                    Kind = DialogueNodeKind.Line,
                    Speaker = definition.Speaker,
                    Text = definition.StepLines[stepIndex, 0],
                    NextNodeId = "line_2"
                },
                new DialogueNodeData
                {
                    Id = "line_2",
                    Kind = DialogueNodeKind.Line,
                    Speaker = definition.Speaker,
                    Text = definition.StepLines[stepIndex, 1],
                    NextNodeId = "end"
                },
                new DialogueNodeData
                {
                    Id = "end",
                    Kind = DialogueNodeKind.End,
                    EndingId = $"{definition.SceneName.ToLowerInvariant()}_step_{stepIndex + 1}",
                    EndingDescription = "本观察步骤完成。"
                }
            };
        }

        private static List<DialogueNodeData> CreateBranchingNodes(
            DemoDefinition definition,
            int stepIndex)
        {
            return new List<DialogueNodeData>
            {
                new DialogueNodeData
                {
                    Id = "line_1",
                    Kind = DialogueNodeKind.Line,
                    Speaker = definition.Speaker,
                    Text = definition.StepLines[stepIndex, 0],
                    NextNodeId = "choice"
                },
                new DialogueNodeData
                {
                    Id = "choice",
                    Kind = DialogueNodeKind.Choice,
                    Text = "选择要查看的记录",
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData { Text = "查看左侧记录", NextNodeId = "left" },
                        new DialogueChoiceData { Text = "查看右侧记录", NextNodeId = "right" }
                    }
                },
                new DialogueNodeData
                {
                    Id = "left",
                    Kind = DialogueNodeKind.Line,
                    Speaker = definition.Speaker,
                    Text = definition.StepLines[stepIndex, 1] + " 左侧记录已归入历史。",
                    NextNodeId = "end_left"
                },
                new DialogueNodeData
                {
                    Id = "right",
                    Kind = DialogueNodeKind.Line,
                    Speaker = definition.Speaker,
                    Text = definition.StepLines[stepIndex, 1] + " 右侧记录揭示了另一种结论。",
                    NextNodeId = "end_right"
                },
                new DialogueNodeData
                {
                    Id = "end_left",
                    Kind = DialogueNodeKind.End,
                    EndingId = $"lab_step_{stepIndex + 1}_left",
                    EndingDescription = "左侧分支完成。"
                },
                new DialogueNodeData
                {
                    Id = "end_right",
                    Kind = DialogueNodeKind.End,
                    EndingId = $"lab_step_{stepIndex + 1}_right",
                    EndingDescription = "右侧分支完成。"
                }
            };
        }

        private static Camera CreateCamera(Color backgroundColor)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            return camera;
        }

        private static void CreateDirectionalLight()
        {
            // 空场景仍保留主光源，保证 Demo 被主人扩展几何体后无需补基础场景对象。
            var lightObject = new GameObject("Directional Light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        private static List<Transform> CreatePathPoints(Transform root)
        {
            var positions = new[]
            {
                new Vector3(0f, 1.2f, -8f),
                new Vector3(1.8f, 1.55f, -7.3f),
                new Vector3(-1.2f, 1.9f, -6.7f),
                new Vector3(0.4f, 1.4f, -6f)
            };
            var rotations = new[]
            {
                Quaternion.Euler(4f, 0f, 0f),
                Quaternion.Euler(3f, -12f, 0f),
                Quaternion.Euler(2f, 14f, 0f),
                Quaternion.Euler(4f, 0f, 0f)
            };
            var result = new List<Transform>();

            for (var index = 0; index < positions.Length; index++)
            {
                // 控制点只保存 Transform，不添加 Renderer，Game 视图中保持完全不可见。
                var point = new GameObject($"Point {index:00}").transform;
                point.SetParent(root, false);
                point.position = positions[index];
                point.rotation = rotations[index];
                result.Add(point);
            }

            return result;
        }

        private static GameObject CreateCanvas(Transform parent)
        {
            var canvas = new GameObject(
                "Dialogue Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvas.transform.SetParent(parent, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void ConfigureDialogueUi(
            Transform root,
            DialogueView view,
            TMP_FontAsset chineseFont)
        {
            // 场景序列化阶段仍使用默认拉丁字体；中文正文会在运行时字体提供器 Awake 后再显示。
            var historyButton = CreateButton(root, "History", "HISTORY", new Vector2(140f, 52f));
            SetAnchors(historyButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -34f));
            var speedButton = CreateButton(root, "Speed", "1X", new Vector2(110f, 52f));
            SetAnchors(speedButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-380f, -34f));
            var autoButton = CreateButton(root, "Auto", "AUTO", new Vector2(140f, 52f));
            SetAnchors(autoButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-245f, -34f));
            var skipButton = CreateButton(root, "Skip", "SKIP", new Vector2(110f, 52f));
            SetAnchors(skipButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, -34f));

            var dialoguePanel = CreatePanel(root, "Dialogue Panel", new Color(0.015f, 0.02f, 0.03f, 0.92f));
            var panelRect = dialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = new Vector2(0f, 235f);
            var speaker = CreateText(dialoguePanel.transform, "Speaker", "NARRATOR", 32f, TextAlignmentOptions.Left);
            SetAnchors(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -46f), new Vector2(260f, 44f));
            var body = CreateText(dialoguePanel.transform, "Body", string.Empty, 36f, TextAlignmentOptions.Left);
            body.enableWordWrapping = true;
            SetStretchOffsets(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(300f, 28f), new Vector2(-64f, -42f));
            var dialogueButton = dialoguePanel.AddComponent<Button>();

            var choicesRoot = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            choicesRoot.transform.SetParent(root, false);
            var choicesRect = choicesRoot.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0.5f, 0f);
            choicesRect.anchorMax = new Vector2(0.5f, 0f);
            choicesRect.anchoredPosition = new Vector2(0f, 285f);
            choicesRect.sizeDelta = new Vector2(720f, 0f);
            choicesRoot.GetComponent<VerticalLayoutGroup>().spacing = 12f;
            choicesRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var choicePanel = choicesRoot.AddComponent<DialogueChoiceListPanel>();
            var choiceTemplate = CreateButton(choicesRoot.transform, "Choice Button Template", "CHOICE", new Vector2(720f, 62f));
            choiceTemplate.gameObject.SetActive(false);
            choicePanel.Configure(choicesRoot.transform, choiceTemplate);

            // 历史面板由共享工厂生成，确保普通 Sample 与三套导览拥有一致的标题、滚动和边缘渐隐体验。
            var historyPanel = DialogueHistoryUiFactory.Create(
                root,
                new Vector2(0.12f, 0.14f),
                new Vector2(0.88f, 0.86f),
                chineseFont);

            SetPrivate(view, "speakerText", speaker);
            SetPrivate(view, "bodyText", body);
            view.ConfigureControlLabels(
                speedButton.GetComponentInChildren<TMP_Text>(),
                autoButton.GetComponentInChildren<TMP_Text>());
            view.BindChoicePanel(choicePanel);
            view.BindHistoryPanel(historyPanel);
            UnityEventTools.AddPersistentListener(speedButton.onClick, view.HandleSpeedClick);
            UnityEventTools.AddPersistentListener(autoButton.onClick, view.HandleAutoClick);
            UnityEventTools.AddPersistentListener(skipButton.onClick, view.HandleSkipClick);
            UnityEventTools.AddPersistentListener(dialogueButton.onClick, view.HandleAdvanceClick);
            UnityEventTools.AddPersistentListener(historyButton.onClick, historyPanel.ToggleVisible);
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            var buttonObject = CreatePanel(parent, name, new Color(0.08f, 0.1f, 0.14f, 0.96f));
            var button = buttonObject.AddComponent<Button>();
            button.GetComponent<RectTransform>().sizeDelta = size;
            var text = CreateText(buttonObject.transform, "Label", label, 23f, TextAlignmentOptions.Center);
            SetStretchOffsets(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.9f, 0.94f, 1f);
            return text;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 min,
            Vector2 max,
            Vector2 position,
            Vector2 size = default)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(
                Mathf.Approximately(min.x, max.x) ? min.x : 0.5f,
                Mathf.Approximately(min.y, max.y) ? min.y : 0.5f);
            rect.anchoredPosition = position;
            if (size != default)
            {
                rect.sizeDelta = size;
            }
        }

        private static void SetStretchOffsets(
            RectTransform rect,
            Vector2 min,
            Vector2 max,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void AddScenesToBuildSettings(IReadOnlyList<string> scenePaths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var path in scenePaths)
            {
                var exists = false;
                foreach (var scene in scenes)
                {
                    if (scene.path == path)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static TMP_FontAsset LoadBundledChineseFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                DialoguePackagePaths.BundledFontAssetPath);
            // 包内字体是只读发布资源；缺失时必须停止生成，避免产生不可显示中文的样例。
            if (fontAsset == null)
            {
                throw new InvalidOperationException(
                    $"[{DialoguePackagePaths.PackageId}] 未找到随包中文字体 "
                    + $"{DialoguePackagePaths.BundledFontAssetPath}，请重新安装插件。 ");
            }

            // 包安装目录可能只读，因此这里只验证已发布字体，不在生成 Demo 时重建或扩充图集。
            if (fontAsset.material == null
                || fontAsset.atlasTextures == null
                || fontAsset.atlasTextures.Length == 0
                || fontAsset.atlasTextures[0] == null)
            {
                throw new InvalidOperationException(
                    $"[{DialoguePackagePaths.PackageId}] 随包中文字体图集不完整，请重新安装插件。 ");
            }

            var requiredCharacters = CollectRequiredFontCharacters();
            // 发布包必须预先包含 Demo 字形，生成器不能在只读安装目录中动态补写 Atlas。
            if (!fontAsset.HasCharacters(requiredCharacters))
            {
                throw new InvalidOperationException(
                    $"[{DialoguePackagePaths.PackageId}] 随包字体缺少 Demo 所需中文字符。 ");
            }

            return fontAsset;
        }

        private static string CollectRequiredFontCharacters()
        {
            var characters = new StringBuilder(
                "中文对话步骤历史选择本观察完成左右分支记录结论");
            foreach (var definition in CreateDefinitions())
            {
                characters.Append(definition.Speaker);
                for (var row = 0; row < definition.StepLines.GetLength(0); row++)
                {
                    for (var column = 0; column < definition.StepLines.GetLength(1); column++)
                    {
                        characters.Append(definition.StepLines[row, column]);
                    }
                }
            }

            characters.Append("故事情节你的选择旁白选择要查看的记录查看左侧记录查看右侧记录左侧记录已归入历史右侧记录揭示了另一种结论本观察步骤完成左右分支完成");
            return characters.ToString();
        }

        private static void SetPrivate(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, name);
            }

            field.SetValue(target, value);
        }

        private static IReadOnlyList<DemoDefinition> CreateDefinitions()
        {
            return new[]
            {
                new DemoDefinition(
                    "01_AncientCityTour",
                    "AncientCityTour",
                    "古城档案",
                    new Color(0.025f, 0.035f, 0.05f),
                    false,
                    new[,]
                    {
                        { "第一观察步骤开始。这里演示中文逐字显示。", "点击正文可先补全当前句，再点击一次进入下一句。" },
                        { "上一段结束后，界面已经淡出，镜头沿隐藏路径来到第二点。", "路径控制点只在 Scene 视图显示，不会污染 Game 画面。" },
                        { "现在来到最后一个观察步骤。", "本段结束后，整个对话 UGUI 将淡出并关闭。" }
                    }),
                new DemoDefinition(
                    "02_AbandonedLabTour",
                    "AbandonedLabTour",
                    "研究所记录",
                    new Color(0.025f, 0.045f, 0.04f),
                    true,
                    new[,]
                    {
                        { "终端中发现两份互相矛盾的记录。", "你的选择会进入不同台词与结局，并写入历史。" },
                        { "第二个终端仍保留两条可调查路径。", "导览控制器只等待整个分支结束，不干预分支内部逻辑。" },
                        { "最后一组记录等待确认。", "完成选择与后续台词后，本 Demo 将关闭界面。" }
                    }),
                new DemoDefinition(
                    "03_RainyStreetTour",
                    "RainyStreetTour",
                    "街区旁白",
                    new Color(0.025f, 0.03f, 0.055f),
                    false,
                    new[,]
                    {
                        { "第三套样例用于体验自动播放、倍速和跳过。", "点击 AUTO 开启自动推进，速度按钮可循环切换一、二、四倍。" },
                        { "SKIP 会快速推进连续台词，并在选择或结局处停止。", "镜头切换与界面淡入淡出始终使用未缩放时间。" },
                        { "这是整套演示的最终观察步骤。", "对话结束后界面不再阻挡射线，场景保持安静。" }
                    })
            };
        }

        private sealed class DemoDefinition
        {
            public DemoDefinition(
                string folderName,
                string sceneName,
                string speaker,
                Color backgroundColor,
                bool isBranching,
                string[,] stepLines)
            {
                FolderName = folderName;
                SceneName = sceneName;
                Speaker = speaker;
                BackgroundColor = backgroundColor;
                IsBranching = isBranching;
                StepLines = stepLines;
            }

            public string FolderName { get; }

            public string SceneName { get; }

            public string Speaker { get; }

            public Color BackgroundColor { get; }

            public bool IsBranching { get; }

            public string[,] StepLines { get; }
        }
    }
}
