# Guided Dialogue Samples Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Assets/DialogueSystem/Samples/` 下交付三套空场景中文对话 Demo，使每段对话结束后镜头沿隐藏样条进入下一步骤，并在最终步骤后淡出、禁用整个 UGUI。

**Architecture:** 新增独立的样条数学与镜头组件、Canvas 淡入淡出组件、步骤流程协调器和运行时中文字体提供器；既有 `DialogueRunner` 与 `DialogueView` 保持职责不变。编辑器生成器确定性创建三套场景和九份中文对话资产，所有功能通过现有事件边界组合，不向对话数据模型加入导览概念。

**Tech Stack:** Unity `2022.3.62f1c1`、C#、UGUI `1.0.0`、TextMeshPro `3.0.7`、Unity Test Framework、MCP for Unity `v10.0.0`。

**Spec:** `docs/superpowers/specs/2026-08-28-guided-dialogue-samples-design.md`

## Global Constraints

- 插件根目录固定为 `Assets/DialogueSystem/`。
- 不新增第三方运行时依赖；不使用 Cinemachine 或 Timeline。
- 三套 Demo 场景不包含建筑、模型、纹理、音频或装饰几何体。
- 每套 Demo 包含三个对话步骤；镜头路径点在运行时不可见。
- 当前 `DialogueSystemSample`、现有运行时代码语义和现有用户资源不得删除。
- 新增代码、复杂逻辑、空值保护和运行时/编辑器分支上方使用中文注释解释原因与意图。
- 当前目录不是 Git 仓库，不初始化 Git；每个任务以测试结果和 Unity Console 状态作为检查点。
- 最终隐藏必须同时满足 Canvas 根对象 inactive、`CanvasGroup.alpha == 0`、`interactable == false`、`blocksRaycasts == false`。

---

## File Structure

```text
Assets/DialogueSystem/
  Runtime/UI/
    DialogueSplineMath.cs              # 无状态 Catmull-Rom 采样
    DialogueCameraSpline.cs            # 相机沿路径点移动与旋转
    DialogueCanvasFader.cs              # CanvasGroup 淡入、淡出和最终关闭
    DialogueTourStep.cs                 # 单个导览步骤的序列化配置
    DialogueTourController.cs           # Runner、镜头和 Canvas 的状态协调
    DialogueChineseFontProvider.cs      # 系统中文字体选择与 TMP 动态字体生命周期
  Editor/
    DialogueTourSampleBuilder.cs        # 生成三套目录、资产和场景
  Tests/EditMode/
    DialogueSplineMathTests.cs
    DialogueChineseFontProviderTests.cs
  Tests/PlayMode/
    DialogueCanvasFaderTests.cs
    DialogueTourControllerPlayModeTests.cs
  Samples/
    01_AncientCityTour/
    02_AbandonedLabTour/
    03_RainyStreetTour/
docs/
  DialogueSystem-Tour-Samples.md
```

---

### Task 1: Catmull-Rom 样条数学与相机移动

**Files:**
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueSplineMath.cs`
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueCameraSpline.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueSplineMathTests.cs`

**Interfaces:**
- Consumes: `IReadOnlyList<Vector3>` 路径点、相邻 `fromIndex/toIndex`、归一化进度。
- Produces: `DialogueSplineMath.EvaluateSegment(IReadOnlyList<Vector3>, int, int, float) -> Vector3`、`DialogueCameraSpline.Configure(Camera, IReadOnlyList<Transform>)`、`SnapToControlPoint(int)`、`MoveToControlPoint(int, float) -> IEnumerator`、`ControlPointCount`、`CurrentControlPointIndex`。

- [ ] **Step 1: 写入失败的样条测试**

```csharp
[Test]
public void EvaluateSegment_UsesExactEndpoints()
{
    var points = new[]
    {
        new Vector3(-2f, 0f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(3f, 1f, 2f),
        new Vector3(5f, 0f, 3f)
    };

    Assert.That(DialogueSplineMath.EvaluateSegment(points, 1, 2, 0f), Is.EqualTo(points[1]));
    Assert.That(DialogueSplineMath.EvaluateSegment(points, 1, 2, 1f), Is.EqualTo(points[2]));
}

[Test]
public void EvaluateSegment_WithTwoPointsFallsBackToLinearInterpolation()
{
    var points = new[] { Vector3.zero, new Vector3(10f, 0f, 0f) };
    Assert.That(DialogueSplineMath.EvaluateSegment(points, 0, 1, 0.25f),
        Is.EqualTo(new Vector3(2.5f, 0f, 0f)));
}
```

同时测试空列表、越界索引抛出 `ArgumentException` 或 `ArgumentOutOfRangeException`，一个点返回唯一位置，输入进度钳制到 `[0, 1]`。

- [ ] **Step 2: 运行测试并确认红灯**

通过 MCP 运行 `DialogueSplineMathTests`。预期编译失败，首个错误为 `DialogueSplineMath` 类型不存在；用 `read_console` 保存错误证据。

- [ ] **Step 3: 实现最小样条数学**

```csharp
public static Vector3 EvaluateSegment(
    IReadOnlyList<Vector3> points,
    int fromIndex,
    int toIndex,
    float t)
{
    if (points == null || points.Count == 0)
        throw new ArgumentException("路径至少需要一个控制点。", nameof(points));
    if (fromIndex < 0 || fromIndex >= points.Count)
        throw new ArgumentOutOfRangeException(nameof(fromIndex));
    if (toIndex < 0 || toIndex >= points.Count)
        throw new ArgumentOutOfRangeException(nameof(toIndex));

    t = Mathf.Clamp01(t);
    if (points.Count == 1 || fromIndex == toIndex) return points[fromIndex];
    if (points.Count == 2) return Vector3.Lerp(points[fromIndex], points[toIndex], t);

    var p0 = points[Mathf.Max(0, fromIndex - 1)];
    var p1 = points[fromIndex];
    var p2 = points[toIndex];
    var p3 = points[Mathf.Min(points.Count - 1, toIndex + 1)];
    var t2 = t * t;
    var t3 = t2 * t;
    return 0.5f * ((2f * p1) + (-p0 + p2) * t
        + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
        + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
}
```

- [ ] **Step 4: 实现镜头组件**

`DialogueCameraSpline` 缓存控制点世界坐标，每帧使用 `Time.unscaledDeltaTime`；位置调用 `EvaluateSegment`，旋转使用 `Quaternion.Slerp`，进度使用 `Mathf.SmoothStep(0f, 1f, elapsed / duration)`。`duration <= 0` 时直接定位，控制点无效时抛出包含索引的错误。`OnDrawGizmosSelected` 以固定采样数绘制路径，不创建运行时 Renderer。

- [ ] **Step 5: 运行样条测试并检查 Console**

运行 `DialogueSplineMathTests`，预期全部通过；读取 Console error，预期零条非测试预期错误。

---

### Task 2: Canvas 淡入淡出与最终关闭

**Files:**
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueCanvasFader.cs`
- Create: `Assets/DialogueSystem/Tests/PlayMode/DialogueCanvasFaderTests.cs`

**Interfaces:**
- Consumes: `CanvasGroup`、未缩放时间、淡入淡出时长。
- Produces: `Configure(CanvasGroup, float)`、`ShowImmediate()`、`HideImmediate(bool)`、`FadeIn() -> IEnumerator`、`FadeOut(bool) -> IEnumerator`、`IsTransitioning`、`IsVisible`。

- [ ] **Step 1: 写入失败的 PlayMode 测试**

```csharp
[UnityTest]
public IEnumerator FadeOut_FinalCloseDisablesCanvasAndRaycasts()
{
    var root = new GameObject("Canvas", typeof(Canvas), typeof(CanvasGroup));
    var fader = root.AddComponent<DialogueCanvasFader>();
    var group = root.GetComponent<CanvasGroup>();
    fader.Configure(group, 0.01f);
    fader.ShowImmediate();

    yield return fader.FadeOut(true);

    Assert.That(group.alpha, Is.Zero);
    Assert.That(group.interactable, Is.False);
    Assert.That(group.blocksRaycasts, Is.False);
    Assert.That(root.activeSelf, Is.False);
    Object.DestroyImmediate(root);
}
```

再测试 `FadeOut(false)` 保持根对象 active、`FadeIn()` 恢复 alpha/交互/射线、第二个转换开始时不会遗留错误状态。

- [ ] **Step 2: 运行测试并确认红灯**

运行 `DialogueCanvasFaderTests`，预期 `DialogueCanvasFader` 类型不存在。

- [ ] **Step 3: 实现 CanvasGroup 状态机**

淡出一开始就设置 `interactable = false`、`blocksRaycasts = false`。淡入先激活根对象，从当前 alpha 插值到 1，完成后才恢复交互。所有协程在 `try/finally` 中恢复 `IsTransitioning = false`；时长为零时同帧设置最终状态。

- [ ] **Step 4: 运行测试并建立检查点**

运行 `DialogueCanvasFaderTests`，预期全部通过；读取 Console error，预期零条错误。

---

### Task 3: 对话步骤协调器

**Files:**
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueTourStep.cs`
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueTourController.cs`
- Create: `Assets/DialogueSystem/Tests/PlayMode/DialogueTourControllerPlayModeTests.cs`

**Interfaces:**
- Consumes: `DialogueRunner.Ended`、`DialogueRunner.Failed`、`DialogueCameraSpline`、`DialogueCanvasFader`、有序步骤列表。
- Produces: `DialogueTourStep(DialogueAsset, int, float, float)`、`DialogueTourController.Configure(DialogueRunner, DialogueCameraSpline, DialogueCanvasFader, IReadOnlyList<DialogueTourStep>)`、`BeginTour()`、`CurrentStepIndex`、`State`；枚举 `DialogueTourState { Idle, Presenting, Transitioning, Completed, Failed }`。

- [ ] **Step 1: 写入失败的流程测试**

```csharp
[UnityTest]
public IEnumerator EndingStep_MovesOnceAndStartsNextDialogue()
{
    var rig = CreateTourRig(stepCount: 2, transitionSeconds: 0.01f);
    rig.Controller.BeginTour();
    Assert.That(rig.Controller.CurrentStepIndex, Is.Zero);

    rig.Runner.Skip();
    yield return new WaitForSecondsRealtime(0.08f);

    Assert.That(rig.Controller.CurrentStepIndex, Is.EqualTo(1));
    Assert.That(rig.Controller.State, Is.EqualTo(DialogueTourState.Presenting));
    Assert.That(rig.Runner.Current.Text, Is.EqualTo("步骤 2"));
    rig.Dispose();
}
```

再覆盖：空步骤进入 `Failed`；最终步骤结束后进入 `Completed` 且 Canvas inactive；Transitioning 期间重复结束调用只前进一次；`OnDestroy` 后 Runner 事件不再访问已销毁组件。

- [ ] **Step 2: 运行测试并确认红灯**

运行 `DialogueTourControllerPlayModeTests`，预期新类型不存在。

- [ ] **Step 3: 实现步骤数据**

`DialogueTourStep` 使用 `[Serializable]` 私有序列化字段并公开只读属性：

```csharp
public DialogueAsset Dialogue { get; }
public int PathPointIndex { get; }
public float MoveDuration { get; }
public float ArrivalDelay { get; }
```

构造函数钳制负时长为零，但不静默修正空 Dialogue 或负索引；由控制器在开始前统一校验并报告具体步骤。

- [ ] **Step 4: 实现导览状态协调**

`BeginTour()` 订阅 Runner、校验依赖、定位首个路径点、显示 Canvas 并启动首个资产。`OnDialogueEnded` 只在 `Presenting` 响应：最终步骤启动 `CompleteTour()`，否则启动 `AdvanceStep()`。

```csharp
private IEnumerator AdvanceStep()
{
    state = DialogueTourState.Transitioning;
    yield return canvasFader.FadeOut(false);
    var nextIndex = currentStepIndex + 1;
    var next = steps[nextIndex];
    yield return cameraSpline.MoveToControlPoint(next.PathPointIndex, next.MoveDuration);
    if (next.ArrivalDelay > 0f)
        yield return new WaitForSecondsRealtime(next.ArrivalDelay);

    currentStepIndex = nextIndex;
    dialogueRunner.StartDialogue(next.Dialogue);
    yield return canvasFader.FadeIn();
    state = DialogueTourState.Presenting;
}
```

加载下一段文本发生在 alpha 为零时，确保淡入过程中不会闪现上一段内容。`OnDialogueFailed` 停止导览、禁止 Canvas 交互并带 Demo 名和步骤索引记录错误。`OnDisable`/`OnDestroy` 退订事件并停止活动协程。

- [ ] **Step 5: 运行流程测试并建立检查点**

运行 `DialogueTourControllerPlayModeTests` 和既有 `DialogueViewPlayModeTests`，预期全部通过；确认既有 Runner 行为未回归。

---

### Task 4: 随包中文 TMP 字体

**Files:**
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueChineseFontProvider.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueChineseFontProviderTests.cs`

**Interfaces:**
- Consumes: OFL 授权的 Noto Sans SC 字体、`TMP_FontAsset.CreateFontAsset(Font, ...)`、Canvas 根 Transform。
- Produces: `Configure(Transform, TMP_FontAsset)`、`ApplyFont()`、`SelectedFontName`，以及 `NotoSansSC-Dynamic.asset`。

- [ ] **Step 1: 写入失败的字体选择测试**

```csharp
[Test]
public void SelectInstalledFont_PrefersMicrosoftYaHei()
{
    var installed = new[] { "SimSun", "Microsoft YaHei", "Noto Sans SC" };
    Assert.That(DialogueChineseFontProvider.SelectInstalledFont(installed),
        Is.EqualTo("Microsoft YaHei"));
}

[Test]
public void SelectInstalledFont_WhenNoCandidateReturnsNull()
{
    Assert.That(DialogueChineseFontProvider.SelectInstalledFont(new[] { "Liberation Sans" }), Is.Null);
}
```

再测试大小写不敏感和完整候选优先级。

- [ ] **Step 2: 运行测试并确认红灯**

运行 `DialogueChineseFontProviderTests`，预期新类型不存在。

- [ ] **Step 3: 实现字体提供器**

从 Google Fonts 官方仓库附带 `NotoSansSC-Variable.ttf` 与 `OFL.txt`。生成器创建 Dynamic atlas、`2048x2048`、multi-atlas 的 `NotoSansSC-Dynamic.asset`，并为 Canvas 下含 inactive 的所有 `TMP_Text` 赋值。

`Awake()` 调用 `ApplyFont()`，保证 `DialogueDemoBootstrap.Start()` 或 `DialogueTourController.Start()` 启动对话前已替换字体。随包字体缺失时只输出一次 warning；运行时不得销毁项目字体资源。

- [ ] **Step 4: 运行字体测试并检查当前系统**

运行 EditMode 字体测试；再通过 MCP 执行只读代码查询当前系统候选字体，确认 Windows 编辑器能选择 `Microsoft YaHei` 或其他中文字体。

---

### Task 5: 生成三套最小化 Demo

**Files:**
- Create: `Assets/DialogueSystem/Editor/DialogueTourSampleBuilder.cs`
- Generate: `Assets/DialogueSystem/Samples/01_AncientCityTour/AncientCityTour.unity`
- Generate: `Assets/DialogueSystem/Samples/01_AncientCityTour/Step01.asset`
- Generate: `Assets/DialogueSystem/Samples/01_AncientCityTour/Step02.asset`
- Generate: `Assets/DialogueSystem/Samples/01_AncientCityTour/Step03.asset`
- Generate: `Assets/DialogueSystem/Samples/02_AbandonedLabTour/AbandonedLabTour.unity`
- Generate: `Assets/DialogueSystem/Samples/02_AbandonedLabTour/Step01.asset`
- Generate: `Assets/DialogueSystem/Samples/02_AbandonedLabTour/Step02.asset`
- Generate: `Assets/DialogueSystem/Samples/02_AbandonedLabTour/Step03.asset`
- Generate: `Assets/DialogueSystem/Samples/03_RainyStreetTour/RainyStreetTour.unity`
- Generate: `Assets/DialogueSystem/Samples/03_RainyStreetTour/Step01.asset`
- Generate: `Assets/DialogueSystem/Samples/03_RainyStreetTour/Step02.asset`
- Generate: `Assets/DialogueSystem/Samples/03_RainyStreetTour/Step03.asset`

**Interfaces:**
- Consumes: Tasks 1–4 组件、既有 `DialogueRunner`、`DialogueView`、`DialogueChoiceListPanel`、`DialogueHistoryPanel`。
- Produces: 菜单命令 `Tools/Dialogue System/Create Guided Tour Samples`，三套可独立播放的场景与九份中文资产。

- [ ] **Step 1: 创建幂等目录与对话资产生成逻辑**

生成器只删除自身固定生成路径下的同名 `.unity` 与 `Step01/02/03.asset`，不删除目录或其他文件。资产内容：

- Ancient City：三份线性资产，每份两句中文台词后进入 End。
- Abandoned Lab：每份包含台词、两项选择、分支台词与 End；至少第一步的两项选择进入不同结局。
- Rainy Street：三份线性资产，文本明确提示 `AUTO`、倍速与 `SKIP` 的体验方式。

- [ ] **Step 2: 创建最小场景与隐藏路径**

每个场景使用 `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)`，仅创建规格列出的对象。主相机、Directional Light、EventSystem 必须存在；Camera Path 创建四个空 Transform，前三个分别绑定三个步骤，第四个只作为末端切线控制点。

- [ ] **Step 3: 创建并绑定 UGUI**

Canvas 使用 `ScreenSpaceOverlay`、参考分辨率 `1920x1080` 和 `CanvasGroup`。界面包含角色名、正文点击区、三个选择按钮容器、历史、倍速、自动和跳过按钮。所有控件使用既有 `DialogueView` 接口绑定；所有中文和英文 TMP 文本都由 `DialogueChineseFontProvider` 在 Awake 替换字体。

- [ ] **Step 4: 配置导览组件并保存**

每套场景配置三个 `DialogueTourStep`，路径点索引为 `0, 1, 2`，移动时长为 `0.8s`，到达停顿为 `0.1s`，淡入淡出为 `0.2s`。保存场景前调用 `AssetDatabase.SaveAssets()`，并把生成场景加入 Build Settings 时只追加缺失项、不移除用户已有场景。

- [ ] **Step 5: 通过 MCP 执行生成菜单**

```text
execute_menu_item(menu_path="Tools/Dialogue System/Create Guided Tour Samples")
refresh_unity(wait_for_ready=true)
read_console(action="get", types=["error"], count=100, format="detailed", include_stacktrace=true)
```

预期菜单成功、九份资产和三份场景存在、Console 零条编译错误。

---

### Task 6: 文档与端到端验收

**Files:**
- Create: `docs/DialogueSystem-Tour-Samples.md`
- Verify: `Assets/DialogueSystem/Samples/01_AncientCityTour/AncientCityTour.unity`
- Verify: `Assets/DialogueSystem/Samples/02_AbandonedLabTour/AbandonedLabTour.unity`
- Verify: `Assets/DialogueSystem/Samples/03_RainyStreetTour/RainyStreetTour.unity`

**Interfaces:**
- Consumes: 全部新增组件、生成器和场景。
- Produces: 使用说明、测试结果、Console 清洁证据和三套场景的运行验收。

- [ ] **Step 1: 编写使用说明**

文档解释目录、打开场景方式、三个 Demo 各自演示内容、步骤与路径点的配置、最终隐藏语义、系统中文字体限制，以及如何通过菜单安全重建生成内容。

- [ ] **Step 2: 运行全部 EditMode 测试**

通过 MCP 运行完整 EditMode 测试集合并等待 job 完成。预期 failed 为 0；若失败，记录测试全名、消息和堆栈，修复后重新运行整个集合。

- [ ] **Step 3: 运行全部 PlayMode 测试**

通过 MCP 运行完整 PlayMode 测试集合并等待 job 完成。预期 failed 为 0；域重载导致孤儿任务时只使用 MCP 的 `clear_stuck` 选项清理测试任务，不删除项目文件。

- [ ] **Step 4: 逐套场景验收**

依次打开三套场景并进入 Play Mode：确认第一段中文出现；完成当前资产后 UI 淡出、镜头移动、下一段淡入；分支选择写入历史；自动播放遇到选择停下；最终步骤完成后 Canvas 根对象 inactive。

- [ ] **Step 5: 最终 Console 与资源检查**

退出 Play Mode，读取 Console error，预期零条。搜索 `Assets/DialogueSystem/Samples/0*` 确认三份场景、九份资产及对应 `.meta` 均存在，并确认原 `DialogueSystemSample` 仍存在且未被生成器删除。

---

## Execution Mode

本任务按当前会话内联执行：用户已明确要求开始实现，且未授权创建子代理。使用 `superpowers:executing-plans` 按 Task 1–6 顺序执行，并在测试或 Unity 编译失败时使用系统化调试流程定位根因。
