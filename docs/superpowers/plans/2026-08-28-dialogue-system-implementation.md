# Unity UGUI 分支对话系统 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Unity `2022.3.62f1c1` 中安装并连接 MCP for Unity，交付可编辑、可测试、支持分支与历史记录的原创黑白科技风 UGUI 对话系统插件。

**Architecture:** `DialogueAsset` 保存稳定 GUID 节点图，纯 C# `DialogueSession` 负责条件、变量、跳转和历史，`DialogueRunner` 将会话包装为 Unity 事件源，`DialogueView` 只处理 UGUI 表现和输入。编辑器校验器与示例生成器均放入 Editor 程序集，运行时构建不携带 UnityEditor 依赖。

**Tech Stack:** Unity 2022.3.62f1c1、C#、UGUI 1.0.0、TextMeshPro 3.0.7、Unity Test Framework、ScriptableObject、CoplayDev MCP for Unity v10.0.0。

**Spec:** `docs/superpowers/specs/2026-08-28-dialogue-system-design.md`

## Global Constraints

- Unity 版本固定为 `2022.3.62f1c1`。
- MCP 包固定为 `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0`。
- 运行时只依赖 UGUI `1.0.0` 与 TextMeshPro `3.0.7`，不得新增其他运行时第三方依赖。
- 插件根目录固定为 `Assets/DialogueSystem/`，运行时、编辑器和测试程序集分离。
- UI 只参考信息布局与黑白科技氛围，不复制《明日方舟》角色、Logo、图标或原始美术。
- 不实现角色立绘、语音、存档、本地化管线、外部 JSON 或节点图编辑器。
- 新增代码、复杂逻辑、空值保护及编辑器/运行时分支必须在相关代码上方写中文原因注释。
- 当前工程没有 Git 仓库；不得擅自初始化。每个任务以测试通过和 Console 清洁作为检查点。若执行前由主人初始化 Git，则使用任务末尾给出的提交信息。

---

## 文件结构锁定

```text
Assets/DialogueSystem/
  Runtime/
    DialogueSystem.Runtime.asmdef
    Data/
      DialogueEnums.cs
      DialogueValue.cs
      DialogueVariableDefinition.cs
      DialogueCondition.cs
      DialogueEffect.cs
      DialogueChoiceData.cs
      DialogueNodeData.cs
      DialogueAsset.cs
    Execution/
      DialoguePresentation.cs
      DialogueHistoryEntry.cs
      DialogueSession.cs
      DialogueRunner.cs
    UI/
      DialogueTextAnimator.cs
      DialogueAutoAdvanceClock.cs
      DialogueView.cs
      DialogueChoicePanel.cs
      DialogueHistoryPanel.cs
      DialogueSkipPanel.cs
  Editor/
    DialogueSystem.Editor.asmdef
    DialogueValidationIssue.cs
    DialogueAssetValidator.cs
    DialogueAssetEditor.cs
    DialogueSampleBuilder.cs
  Tests/
    EditMode/
      DialogueSystem.EditModeTests.asmdef
      DialogueTestAssetFactory.cs
      DialogueConditionTests.cs
      DialogueAssetValidatorTests.cs
      DialogueAssetEditorTests.cs
      DialogueSessionTests.cs
      DialogueSkipTests.cs
    PlayMode/
      DialogueSystem.PlayModeTests.asmdef
      DialogueTextAnimatorTests.cs
      DialogueViewPlayModeTests.cs
  Art/
    DialogueBackground.png
    DialoguePanelGradient.png
  Prefabs/
    DialogueCanvas.prefab
  Samples/
    Dialogue/
      SampleBranchingDialogue.asset
    Scenes/
      DialogueDemo.unity
docs/
  DialogueSystem-Usage.md
```

程序集引用方向固定为：`Runtime ← EditModeTests`、`Runtime ← PlayModeTests`、`Runtime ← Editor`。Runtime 不得引用 Editor 或 Tests。

---

### Task 1: 安装并验证 MCP for Unity

**Files:**
- Modify through Unity Package Manager: `Packages/manifest.json`
- Modify through Unity Package Manager: `Packages/packages-lock.json`
- External configuration created by MCP installer: `C:/Users/zxxuh/.codex/config.toml`

**Interfaces:**
- Consumes: Unity `2022.3.62f1c1`、Codex Desktop、可访问 GitHub 的网络环境。
- Produces: 可调用的 `unityMCP` stdio 服务，以及 `read_console`、`manage_scene`、`run_tests`、`get_test_job`、`execute_menu_item` 工具。

- [ ] **Step 1: 记录安装前状态**

  读取 `Packages/manifest.json` 和 `Packages/packages-lock.json`，确认没有 `com.coplaydev.unity-mcp`；运行 `node --version`、`python --version`、`uv --version`，记录存在的运行时。缺少 `uv` 时使用 Unity MCP 窗口提供的安装操作，不手工下载未知二进制。

- [ ] **Step 2: 通过 Unity Package Manager 安装固定版本**

  打开 `Window → Package Manager → + → Add package from git URL`，输入：

  ```text
  https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0
  ```

  等待 Package Manager 完成解析和脚本编译。预期 `manifest.json` 出现 `com.coplaydev.unity-mcp` Git 依赖，Console 无编译错误。

- [ ] **Step 3: 安装服务器依赖并配置 Codex**

  打开 `Window → MCP for Unity`，选择 stdio 传输，执行服务器依赖安装，然后点击 `Configure All Detected Clients`。在写入 `C:/Users/zxxuh/.codex/config.toml` 前保留插件自动生成的原文件备份；配置块名称应为 `mcp_servers.unityMCP`，启动超时至少 60 秒。

- [ ] **Step 4: 重启 Codex 并验证工具发现**

  重启 Codex Desktop 后回到本任务。读取 MCP 资源 `project_info` 与 `unity_instances`，确认项目路径为 `D:/demo/Dialogue System Plugin`、版本为 `2022.3.62f1c1`，并执行：

  ```text
  read_console(action="get", types=["error"], count=20, format="detailed", include_stacktrace=true)
  manage_scene(action="get_active")
  ```

  预期工具成功返回且 Console 没有编译错误。

- [ ] **Step 5: 建立检查点**

  保存安装后 `manifest.json` 与 `packages-lock.json` 的差异。若工程届时已初始化 Git，提交信息使用：

  ```text
  chore: install MCP for Unity v10.0.0
  ```

---

### Task 2: 建立运行时程序集和对话数据模型

**Files:**
- Create: `Assets/DialogueSystem/Runtime/DialogueSystem.Runtime.asmdef`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueEnums.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueValue.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueVariableDefinition.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueCondition.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueEffect.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueChoiceData.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueNodeData.cs`
- Create: `Assets/DialogueSystem/Runtime/Data/DialogueAsset.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueSystem.EditModeTests.asmdef`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueTestAssetFactory.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueConditionTests.cs`

**Interfaces:**
- Consumes: Unity serialization、NUnit。
- Produces: `DialogueValue.FromBool(bool)`、`DialogueValue.FromInt(int)`、`DialogueCondition.IsMet(IReadOnlyDictionary<string, DialogueValue>)`、`DialogueEffect.Apply(IDictionary<string, DialogueValue>)`、`DialogueAsset.EntryNodeId`、`DialogueAsset.Nodes`。

- [ ] **Step 1: 创建程序集定义**

  Runtime asmdef 名称使用 `DialogueSystem.Runtime`，引用 `Unity.TextMeshPro` 与 `Unity.ugui`，关闭 Unsafe。EditMode 测试 asmdef 名称使用 `DialogueSystem.EditModeTests`，引用 `DialogueSystem.Runtime`、`UnityEngine.TestRunner`、`UnityEditor.TestRunner`，并限定为 Editor 平台。

- [ ] **Step 2: 写入失败的条件与效果测试**

  `DialogueConditionTests.cs` 至少包含以下测试：

  ```csharp
  [Test]
  public void BoolCondition_MatchesExpectedValue()
  {
      var values = new Dictionary<string, DialogueValue>
      {
          ["trusted"] = DialogueValue.FromBool(true)
      };
      var condition = DialogueTestAssetFactory.BoolCondition("trusted", true);

      Assert.That(condition.IsMet(values), Is.True);
  }

  [TestCase(DialogueComparison.Equal, 3, 3, true)]
  [TestCase(DialogueComparison.Greater, 4, 3, true)]
  [TestCase(DialogueComparison.LessOrEqual, 4, 3, false)]
  public void IntCondition_UsesConfiguredComparison(
      DialogueComparison comparison, int actual, int expected, bool result)
  {
      var values = new Dictionary<string, DialogueValue>
      {
          ["score"] = DialogueValue.FromInt(actual)
      };
      var condition = DialogueTestAssetFactory.IntCondition("score", comparison, expected);

      Assert.That(condition.IsMet(values), Is.EqualTo(result));
  }

  [Test]
  public void AddIntEffect_ChangesExistingValue()
  {
      IDictionary<string, DialogueValue> values = new Dictionary<string, DialogueValue>
      {
          ["score"] = DialogueValue.FromInt(2)
      };

      DialogueTestAssetFactory.AddIntEffect("score", 3).Apply(values);

      Assert.That(values["score"].IntValue, Is.EqualTo(5));
  }
  ```

- [ ] **Step 3: 通过 MCP 运行测试并确认红灯**

  调用 `run_tests(mode="EditMode", assembly_names="DialogueSystem.EditModeTests", include_failed_tests=true)`，再用 `get_test_job(job_id, include_failed_tests=true, wait_timeout=60)` 等待结果。预期因类型尚不存在而编译失败；用 `read_console` 保存首个编译错误作为红灯证据。

- [ ] **Step 4: 实现枚举和值类型**

  `DialogueEnums.cs` 定义：

  ```csharp
  public enum DialogueNodeKind { Line, Choice, End }
  public enum DialogueValueKind { Bool, Int }
  public enum DialogueComparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
  public enum DialogueEffectOperation { SetBool, SetInt, AddInt }
  ```

  `DialogueValue` 使用只读属性 `Kind`、`BoolValue`、`IntValue`，只能通过 `FromBool` 与 `FromInt` 创建。读取错误类型的值时抛出带变量类型信息的 `InvalidOperationException`。

- [ ] **Step 5: 实现可序列化数据类**

  创建以下精确接口：

  ```csharp
  [Serializable]
  public sealed class DialogueVariableDefinition
  {
      public string Key;
      public DialogueValueKind Kind;
      public bool BoolValue;
      public int IntValue;
      public DialogueValue CreateValue();
  }

  [Serializable]
  public sealed class DialogueCondition
  {
      public string VariableKey;
      public DialogueComparison Comparison;
      public bool BoolValue;
      public int IntValue;
      public bool IsMet(IReadOnlyDictionary<string, DialogueValue> values);
  }

  [Serializable]
  public sealed class DialogueEffect
  {
      public string VariableKey;
      public DialogueEffectOperation Operation;
      public bool BoolValue;
      public int IntValue;
      public void Apply(IDictionary<string, DialogueValue> values);
  }
  ```

  缺失变量、类型不匹配和整数操作作用于布尔值时抛出包含 `VariableKey` 的 `InvalidOperationException`。

- [ ] **Step 6: 实现节点和资产**

  `DialogueNodeData` 包含 `Id`、`Kind`、`Speaker`、`Text`、`NextNodeId`、`List<DialogueChoiceData> Choices`、`EndingId`、`EndingDescription`。`DialogueChoiceData` 包含 `Text`、`Conditions`、`Effects`、`NextNodeId`。`DialogueAsset` 使用 `[CreateAssetMenu(menuName = "Dialogue System/Dialogue Asset")]`，公开只读属性并提供：

  ```csharp
  public bool TryGetNode(string id, out DialogueNodeData node);
  public IReadOnlyDictionary<string, DialogueValue> CreateInitialValues();
  ```

  字典在 `OnEnable` 和首次访问时重建，重复 ID 不静默覆盖，交由校验器报告。

- [ ] **Step 7: 运行测试并建立检查点**

  重复 Task 2 Step 3 的 MCP 测试调用。预期 `DialogueConditionTests` 全部通过，随后 `read_console` 返回零条 error。Git 可用时提交：

  ```text
  feat: add dialogue data model
  ```

---

### Task 3: 实现对话资产静态校验

**Files:**
- Create: `Assets/DialogueSystem/Editor/DialogueSystem.Editor.asmdef`
- Create: `Assets/DialogueSystem/Editor/DialogueValidationIssue.cs`
- Create: `Assets/DialogueSystem/Editor/DialogueAssetValidator.cs`
- Modify: `Assets/DialogueSystem/Tests/EditMode/DialogueSystem.EditModeTests.asmdef`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueAssetValidatorTests.cs`

**Interfaces:**
- Consumes: `DialogueAsset.EntryNodeId`、`DialogueAsset.Nodes`、变量定义与节点目标 GUID。
- Produces: `IReadOnlyList<DialogueValidationIssue> DialogueAssetValidator.Validate(DialogueAsset asset)`；问题包含 `Severity`、`Code`、`Message`、`NodeId`。

- [ ] **Step 1: 写入失败的校验测试**

  覆盖以下独立情况，并断言稳定错误码：

  ```csharp
  Assert.That(IssueCodes(asset), Does.Contain("DIALOGUE_DUPLICATE_NODE_ID"));
  Assert.That(IssueCodes(asset), Does.Contain("DIALOGUE_MISSING_ENTRY"));
  Assert.That(IssueCodes(asset), Does.Contain("DIALOGUE_BROKEN_LINK"));
  Assert.That(IssueCodes(asset), Does.Contain("DIALOGUE_UNREACHABLE_NODE"));
  Assert.That(IssueCodes(asset), Does.Contain("DIALOGUE_UNKNOWN_VARIABLE"));
  Assert.That(IssueCodes(asset), Does.Contain("DIALOGUE_EMPTY_CHOICE_SET"));
  ```

  再写一个有效双结局资产测试，断言 `Validate` 返回空列表。

  同时把 `DialogueSystem.Editor` 加入 EditMode 测试 asmdef 的 references，确保测试通过公开编辑器 API 调用校验器。

- [ ] **Step 2: 运行 EditMode 测试确认红灯**

  只运行 `DialogueSystem.EditModeTests.DialogueAssetValidatorTests`，预期因 `DialogueAssetValidator` 不存在而失败。

- [ ] **Step 3: 实现确定性校验器**

  校验顺序固定为：空资产、入口、节点 ID、跳转目标、变量引用、节点内容、从入口进行 BFS 可达性。使用：

  ```csharp
  public static IReadOnlyList<DialogueValidationIssue> Validate(DialogueAsset asset)
  {
      var issues = new List<DialogueValidationIssue>();
      ValidateAssetReference(asset, issues);
      if (asset == null) return issues;
      var uniqueNodes = ValidateNodeIds(asset, issues);
      ValidateEntry(asset, uniqueNodes, issues);
      ValidateLinksAndVariables(asset, uniqueNodes, issues);
      ValidateReachability(asset, uniqueNodes, issues);
      return issues;
  }
  ```

  BFS 只跟随合法目标，避免断链造成异常；重复节点 ID 的后续实例不加入图。

- [ ] **Step 4: 运行测试并建立检查点**

  运行整个 EditMode 测试程序集，预期全部通过且 Console 无 error。Git 可用时提交：

  ```text
  feat: validate dialogue assets
  ```

---

### Task 4: 实现纯 C# 分支会话、变量、历史和跳过

**Files:**
- Create: `Assets/DialogueSystem/Runtime/Execution/DialoguePresentation.cs`
- Create: `Assets/DialogueSystem/Runtime/Execution/DialogueHistoryEntry.cs`
- Create: `Assets/DialogueSystem/Runtime/Execution/DialogueSession.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueSessionTests.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueSkipTests.cs`

**Interfaces:**
- Consumes: Task 2 数据模型。
- Produces: `DialogueSession.Start(DialogueAsset)`、`Advance()`、`SelectChoice(int)`、`SkipToDecisionOrEnd(int maxSteps = 10000)`、`Current`、`History`、`IsEnded`、`EndingId`。

- [ ] **Step 1: 写入失败的分支会话测试**

  构建“入口台词 → 两个选项 → 两个结局”的内存资产并断言：

  ```csharp
  session.Start(asset);
  Assert.That(session.Current.Kind, Is.EqualTo(DialogueNodeKind.Line));
  Assert.That(session.Current.Text, Is.EqualTo("通讯接入。"));

  session.Advance();
  Assert.That(session.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));

  session.SelectChoice(1);
  Assert.That(session.IsEnded, Is.True);
  Assert.That(session.EndingId, Is.EqualTo("decline"));
  Assert.That(session.History.Last().Kind, Is.EqualTo(DialogueHistoryKind.Choice));
  ```

  另测条件过滤、效果在跳转前生效、选择越界、结束后推进和没有可用选项。

- [ ] **Step 2: 写入失败的跳过测试**

  创建“台词 A → 台词 B → 选择”与“台词 A → 结局”两条路径：

  ```csharp
  session.Start(asset);
  var result = session.SkipToDecisionOrEnd();

  Assert.That(result, Is.EqualTo(DialogueSkipResult.ReachedChoice));
  Assert.That(session.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));
  Assert.That(session.History.Select(x => x.Text), Does.Contain("台词 B"));
  ```

  再构造自循环节点，断言超过步数时抛出包含节点 ID 的 `InvalidOperationException`。

- [ ] **Step 3: 运行指定测试确认红灯**

  通过 MCP 运行 `DialogueSessionTests` 与 `DialogueSkipTests`，保存首个失败结果。

- [ ] **Step 4: 实现会话状态对象**

  `DialoguePresentation` 是 UI 可读快照，含 `Kind`、`Speaker`、`Text`、只读可见选项列表和可选结局信息。`DialogueSession` 的公共入口为：

  ```csharp
  public void Start(DialogueAsset asset);
  public void Advance();
  public void SelectChoice(int visibleChoiceIndex);
  public DialogueSkipResult SkipToDecisionOrEnd(int maxSteps = 10000);
  ```

  `MoveTo(string nodeId)` 每次只发布一个稳定状态。选择项使用“可见索引 → 原数据选择”映射，确保隐藏选项不会导致选错效果。

- [ ] **Step 5: 实现历史与跳过语义**

  台词首次进入时写入历史；玩家选择后写入选择文字；重复读取 `Current` 不追加。跳过循环只调用内部完成当前台词并移动的方法，在 Choice 或 End 状态返回：

  ```csharp
  public enum DialogueSkipResult { ReachedChoice, ReachedEnd }
  public enum DialogueHistoryKind { Line, Choice }
  ```

  跳过计数达到 `maxSteps` 时抛出错误，消息包含资产名、当前节点 ID 和限制值。

- [ ] **Step 6: 运行测试并建立检查点**

  运行整个 EditMode 程序集。预期条件、校验、会话和跳过测试全部通过。Git 可用时提交：

  ```text
  feat: add branching dialogue session
  ```

---

### Task 5: 添加 Unity 运行器事件边界

**Files:**
- Create: `Assets/DialogueSystem/Runtime/Execution/DialogueRunner.cs`
- Modify: `Assets/DialogueSystem/Tests/EditMode/DialogueSessionTests.cs`

**Interfaces:**
- Consumes: `DialogueSession`。
- Produces: `DialogueRunner.StartDialogue(DialogueAsset)`、`Advance()`、`SelectChoice(int)`、`Skip()`、`Restart()`、`Current`；事件 `Presented`、`HistoryChanged`、`Ended`、`Failed`；序列化字段 `startupDialogue` 与 `playOnStart`。

- [ ] **Step 1: 写入失败的运行器事件测试**

  创建 inactive GameObject 并添加 `DialogueRunner`，订阅事件后调用 `StartDialogue`：

  ```csharp
  DialoguePresentation seen = null;
  runner.Presented += value => seen = value;

  runner.StartDialogue(asset);

  Assert.That(seen.Text, Is.EqualTo("通讯接入。"));
  Assert.That(runner.IsRunning, Is.True);
  ```

  再断言坏资产触发一次 `Failed`，且不会同时触发 `Presented`。

- [ ] **Step 2: 运行测试确认红灯**

  运行新增运行器测试，预期 `DialogueRunner` 不存在。

- [ ] **Step 3: 实现 MonoBehaviour 包装器**

  `DialogueRunner` 持有唯一 `DialogueSession`，所有公共操作使用统一保护：

  ```csharp
  private void ExecuteSafely(Action operation)
  {
      try
      {
          operation();
          PublishState();
      }
      catch (Exception exception)
      {
          IsRunning = false;
          Failed?.Invoke(exception.Message);
          Debug.LogError($"[DialogueSystem] {exception}", this);
      }
  }
  ```

  `PublishState` 先发送 `Presented`，再发送历史快照；End 状态只发送一次 `Ended`。`Restart` 使用最近一次成功启动的资产。

  `Start()` 仅在 `playOnStart` 为真且 `startupDialogue` 非空时调用 `StartDialogue(startupDialogue)`；空资产保护必须在调用上方说明这是为了让 Prefab 可以脱离示例资产复用。

- [ ] **Step 4: 运行测试并建立检查点**

  运行 EditMode 测试并读取 Console。预期测试全绿，只有专门验证错误路径的测试可捕获预期日志。Git 可用时提交：

  ```text
  feat: expose dialogue runner events
  ```

---

### Task 6: 实现打字机与自动推进时钟

**Files:**
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueTextAnimator.cs`
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueAutoAdvanceClock.cs`
- Create: `Assets/DialogueSystem/Tests/PlayMode/DialogueSystem.PlayModeTests.asmdef`
- Create: `Assets/DialogueSystem/Tests/PlayMode/DialogueTextAnimatorTests.cs`

**Interfaces:**
- Consumes: 每帧未缩放时间 `deltaTime`。
- Produces: `DialogueTextAnimator.Begin(int visibleCharacterCount)`、`Tick(float, float)`、`Complete()`、`VisibleCharacterCount`、`IsComplete`；`DialogueAutoAdvanceClock.Begin(int visibleCharacterCount, float speedMultiplier)`、`Tick(float)`、`Pause()`、`Resume()`、`IsReady`。

- [ ] **Step 1: 写入失败的打字机测试**

  ```csharp
  [Test]
  public void Tick_RevealsCharactersAtConfiguredRate()
  {
      var animator = new DialogueTextAnimator(20f);
      animator.Begin(6);

      animator.Tick(0.1f, 1f);

      Assert.That(animator.VisibleCharacterCount, Is.EqualTo(2));
      Assert.That(animator.IsComplete, Is.False);
  }

  [Test]
  public void Complete_RevealsEntireLineImmediately()
  {
      var animator = new DialogueTextAnimator(20f);
      animator.Begin(4);
      animator.Complete();
      Assert.That(animator.VisibleCharacterCount, Is.EqualTo(4));
  }
  ```

  添加 2×/4×、空文本、富文本标签不计为可见字符的测试。富文本计数使用 TMP 解析后的 `textInfo.characterCount`，因此 `DialogueView` 在 Begin 后把解析字符数传给 animator。

- [ ] **Step 2: 写入失败的自动推进测试**

  基础延迟固定 `0.65s`，每个可见字符增加 `0.025s`，速度倍数除以总等待时间。断言 Pause 后 Tick 不累计、Resume 后继续、Reset 后不就绪。

- [ ] **Step 3: 运行 PlayMode 测试确认红灯**

  调用 `run_tests(mode="PlayMode", assembly_names="DialogueSystem.PlayModeTests", init_timeout=120000, include_failed_tests=true)`，再用 `get_test_job(..., wait_timeout=60)` 取得失败。

- [ ] **Step 4: 实现两个无组件状态机**

  `DialogueTextAnimator` 使用浮点累计字符数并钳制到总数；构造函数拒绝小于等于零的基础速率。`DialogueAutoAdvanceClock` 保存目标时长、已用时和暂停标志：

  ```csharp
  public void Begin(int visibleCharacters, float speedMultiplier)
  {
      targetSeconds = (0.65f + visibleCharacters * 0.025f) / speedMultiplier;
      elapsedSeconds = 0f;
      isPaused = false;
  }
  ```

- [ ] **Step 5: 运行测试并建立检查点**

  PlayMode 测试全绿后读取 Console error。Git 可用时提交：

  ```text
  feat: add dialogue timing state machines
  ```

---

### Task 7: 实现 UGUI 主视图、选择、历史与跳过面板

**Files:**
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueView.cs`
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueChoicePanel.cs`
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueHistoryPanel.cs`
- Create: `Assets/DialogueSystem/Runtime/UI/DialogueSkipPanel.cs`
- Create: `Assets/DialogueSystem/Tests/PlayMode/DialogueViewPlayModeTests.cs`

**Interfaces:**
- Consumes: `DialogueRunner` 事件、Task 6 两个状态机、TMP_Text、Button、ScrollRect、CanvasGroup。
- Produces: `DialogueView.Bind(DialogueRunner)`、`SetSpeedIndex(int)`、`ToggleAuto()`、`HandleAdvanceClick()`；三个面板的 `Show`/`Hide` 接口。

- [ ] **Step 1: 写入失败的 PlayMode 交互测试**

  用测试代码创建最小 Canvas、TMP_Text、Button 和视图，覆盖：

  ```csharp
  view.Bind(runner);
  runner.StartDialogue(asset);
  yield return null;

  Assert.That(bodyText.text, Is.EqualTo("通讯接入。"));
  Assert.That(bodyText.maxVisibleCharacters, Is.LessThan(bodyText.textInfo.characterCount));

  view.HandleAdvanceClick();
  Assert.That(bodyText.maxVisibleCharacters, Is.EqualTo(bodyText.textInfo.characterCount));

  view.HandleAdvanceClick();
  Assert.That(runner.Current.Kind, Is.EqualTo(DialogueNodeKind.Choice));
  ```

  再覆盖倍速循环、自动模式遇到 Choice 不推进、历史打开时暂停、跳过取消不调用 Runner、确认后调用一次 Skip。

- [ ] **Step 2: 运行 PlayMode 测试确认红灯**

  只运行 `DialogueViewPlayModeTests`，预期视图类型不存在。

- [ ] **Step 3: 实现 DialogueView 状态协调**

  速度表固定为：

  ```csharp
  private static readonly float[] SpeedMultipliers = { 1f, 2f, 4f };
  ```

  `OnPresented` 根据 Kind 切换面板。Line 状态先设置 TMP 文本、调用 `ForceMeshUpdate()`，再用 `textInfo.characterCount` 启动打字机。`HandleAdvanceClick` 必须遵循“未完成只补全，已完成才推进”。Update 使用 `Time.unscaledDeltaTime`，使历史/遮罩不依赖游戏时间缩放。

- [ ] **Step 4: 实现 ChoicePanel**

  复用按钮池，避免每次选择都销毁对象。接口为：

  ```csharp
  public void Show(IReadOnlyList<DialogueChoicePresentation> choices, Action<int> onSelected);
  public void Hide();
  ```

  每个按钮闭包复制局部索引，防止循环变量捕获错误；显示选择时关闭普通点击推进区。

- [ ] **Step 5: 实现 HistoryPanel 与 SkipPanel**

  `DialogueHistoryPanel.Show(IReadOnlyList<DialogueHistoryEntry>)` 重建可滚动记录并在下一帧将 `ScrollRect.verticalNormalizedPosition` 设为 0。`DialogueSkipPanel.Show(Action confirm, Action cancel)` 每次先移除旧监听再绑定新监听，遮罩启用时阻止点击穿透。

- [ ] **Step 6: 运行测试并建立检查点**

  运行 PlayMode 程序集并读取 Console。预期所有交互测试通过且无 NullReferenceException。Git 可用时提交：

  ```text
  feat: add UGUI dialogue view
  ```

---

### Task 8: 实现 ScriptableObject 自定义 Inspector

**Files:**
- Create: `Assets/DialogueSystem/Editor/DialogueAssetEditor.cs`
- Create: `Assets/DialogueSystem/Tests/EditMode/DialogueAssetEditorTests.cs`

**Interfaces:**
- Consumes: `DialogueAssetValidator.Validate`、SerializedObject API。
- Produces: 节点卡片增删复制、GUID 生成、目标节点下拉框、校验报告和引用保护。

- [ ] **Step 1: 写入失败的 Editor 测试**

  通过内部静态辅助接口测试不依赖绘制坐标的行为：

  ```csharp
  var id = DialogueAssetEditorModel.CreateUniqueNodeId(existingIds);
  Assert.That(id, Is.Not.Empty);
  Assert.That(existingIds, Does.Not.Contain(id));

  var references = DialogueAssetEditorModel.FindReferences(asset, targetNodeId);
  Assert.That(references.Select(x => x.SourceNodeId), Does.Contain("entry"));
  ```

  同时断言节点下拉列表按“类型 + 文本摘要 + 短 GUID”生成稳定标签。

- [ ] **Step 2: 运行 EditMode 测试确认红灯**

  运行 `DialogueAssetEditorTests`，预期编辑器模型不存在。

- [ ] **Step 3: 实现 Inspector 模型与绘制**

  `DialogueAssetEditorModel` 提供纯静态 GUID、引用查找和标签生成。`DialogueAssetEditor.OnInspectorGUI` 使用 SerializedProperty 绘制变量与节点卡片；新增节点使用 `Guid.NewGuid().ToString("N")`。目标字段通过 GenericMenu 写回真实 GUID，不保存数组索引。

- [ ] **Step 4: 实现删除保护与校验面板**

  删除被引用节点前调用 `EditorUtility.DisplayDialog`，正文列出最多 8 个引用源；取消时不修改资产。点击“校验剧情”后调用校验器，并用 HelpBox 按 Error/Warning 显示 Code、节点短 GUID 和 Message。

- [ ] **Step 5: 在 Unity 中手工验证 Inspector**

  创建临时 DialogueAsset，新增三种节点，重排节点，确认 GUID 不变；配置下拉跳转；尝试删除被引用节点并取消；运行校验。完成后只删除该临时资产，不触碰示例资产。

- [ ] **Step 6: 运行测试并建立检查点**

  EditMode 测试全绿、Console 无 error。Git 可用时提交：

  ```text
  feat: add dialogue asset inspector
  ```

---

### Task 9: 生成原创 UI 素材、Prefab、示例剧情和场景

**Files:**
- Create: `Assets/DialogueSystem/Editor/DialogueSampleBuilder.cs`
- Generate through Unity Editor APIs: `Assets/DialogueSystem/Art/DialogueBackground.png`
- Generate through Unity Editor APIs: `Assets/DialogueSystem/Art/DialoguePanelGradient.png`
- Generate through Unity Editor APIs: `Assets/DialogueSystem/Prefabs/DialogueCanvas.prefab`
- Generate through Unity Editor APIs: `Assets/DialogueSystem/Samples/Dialogue/SampleBranchingDialogue.asset`
- Generate through Unity Editor APIs: `Assets/DialogueSystem/Samples/Scenes/DialogueDemo.unity`

**Interfaces:**
- Consumes: Runtime组件、自定义数据模型、UnityEditor 场景/Prefab/AssetDatabase API。
- Produces: 菜单命令 `Dialogue System/Build Sample`，可重复执行并得到相同路径的有效演示内容。

- [ ] **Step 1: 实现确定性的原创纹理生成**

  `DialogueSampleBuilder` 创建 `1920×1080` 黑灰渐变背景与 `1920×320` 透明字幕渐变。像素算法只使用灰阶、透明度、斜线和矩形网格：

  ```csharp
  var vertical = Mathf.InverseLerp(0f, height - 1f, y);
  var baseGray = Mathf.Lerp(0.035f, 0.12f, vertical);
  var grid = (x % 160 < 2 || y % 160 < 2) ? 0.035f : 0f;
  pixels[y * width + x] = new Color(baseGray + grid, baseGray + grid, baseGray + grid, 1f);
  ```

  写入 PNG 后设置 TextureImporter 为 Sprite、关闭 mipmap、启用 alpha，并调用 SaveAndReimport。

- [ ] **Step 2: 创建多分支示例资产**

  示例包含布尔变量 `hasClearance=false`、整数变量 `trust=0`，流程为：

  ```text
  line_intro → choice_access
  choice_access/说明来意 → line_explain → choice_trust
  choice_access/保持沉默 → end_decline
  choice_trust/提交凭证 [trust >= 1] → end_authorized
  choice_trust/暂时离开 → end_wait
  ```

  “说明来意”效果将 `trust` 加 1。最终至少有 `authorized`、`wait`、`decline` 三个结局，保证可以验收条件、效果与多结局。

- [ ] **Step 3: 创建响应式 DialogueCanvas Prefab**

  使用 Editor API 创建 Canvas、CanvasScaler、GraphicRaycaster、EventSystem、全屏背景、底部字幕渐变、角色名、正文、左上记录、右上倍速/自动/跳过、右下选择容器、历史遮罩、跳过确认遮罩。CanvasScaler 精确设置：

  ```csharp
  scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
  scaler.referenceResolution = new Vector2(1920f, 1080f);
  scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
  scaler.matchWidthOrHeight = 0.5f;
  ```

  字体使用 TMP 默认字体资源；文字颜色、按钮边框和科技线条保持黑、白、灰，不导入参考图资源。

- [ ] **Step 4: 创建演示场景**

  新建 `DialogueDemo.unity`，实例化 Prefab，将 `DialogueRunner.startupDialogue` 指向示例资产、将 `playOnStart` 设为 true，并把 `DialogueView` 绑定到同一 Runner。进入 Play Mode 后由 Task 5 已定义的 `DialogueRunner.Start()` 自动开始示例。

- [ ] **Step 5: 通过 MCP 调用生成器**

  等待编译完成后调用：

  ```text
  execute_menu_item(menu_path="Dialogue System/Build Sample")
  refresh_unity(wait_for_ready=true)
  read_console(action="get", types=["error"], count=50, format="detailed", include_stacktrace=true)
  manage_scene(action="open", path="Assets/DialogueSystem/Samples/Scenes/DialogueDemo.unity")
  ```

  预期菜单执行成功、生成资产全部存在、场景打开且 Console 无 error。

- [ ] **Step 6: 建立检查点**

  检查所有生成资源都有对应 `.meta`，Prefab 无 Missing Script，示例资产通过校验器。Git 可用时提交：

  ```text
  feat: add dialogue demo UI and sample story
  ```

---

### Task 10: 完成端到端测试、视觉验收与使用文档

**Files:**
- Modify: `Assets/DialogueSystem/Tests/PlayMode/DialogueViewPlayModeTests.cs`
- Create: `docs/DialogueSystem-Usage.md`
- Verify: `Assets/DialogueSystem/Samples/Scenes/DialogueDemo.unity`

**Interfaces:**
- Consumes: 全部运行时、编辑器、Prefab、示例资产和 MCP 工具。
- Produces: 自动化测试报告、三种宽高比截图检查记录、用户使用说明。

- [ ] **Step 1: 添加端到端 PlayMode 测试**

  加载演示场景，等待 UI 出现，完成以下路径：

  ```text
  补全入口台词 → 推进 → 选择“说明来意” → 推进 → 选择“提交凭证” → authorized
  ```

  断言历史包含入口台词、两次玩家选择和中间台词；重启后走“保持沉默”，断言结局为 `decline` 且历史已清空重建。

- [ ] **Step 2: 运行全部 EditMode 测试**

  ```text
  run_tests(mode="EditMode", include_failed_tests=true, include_details=true)
  get_test_job(job_id=run_tests 返回的 job_id, include_failed_tests=true, include_details=true, wait_timeout=60)
  ```

  预期 failed 为 0。若有失败，保存测试全名、消息和堆栈，修复后重新运行整个 EditMode 集合。

- [ ] **Step 3: 运行全部 PlayMode 测试**

  ```text
  run_tests(mode="PlayMode", init_timeout=120000, include_failed_tests=true, include_details=true)
  get_test_job(job_id=run_tests 返回的 job_id, include_failed_tests=true, include_details=true, wait_timeout=60)
  ```

  预期 failed 为 0。PlayMode 域重载导致任务丢失时，只使用 `run_tests(clear_stuck=true)` 清除孤儿任务，再重新运行一次。

- [ ] **Step 4: 执行三种宽高比视觉检查**

  在 Game View 分别检查 `1920×1080`、`1920×1200`、`2560×1080`。每种比例验证：角色名和正文不裁切、右上按钮不重叠、选择按钮位于安全区域、历史可滚动、跳过确认框居中、无点击穿透。保存三张截图到任务交付说明，不把临时截图导入 Runtime 目录。

- [ ] **Step 5: 执行完整人工交互验收**

  依次验证逐字、第一次点击补全、第二次点击推进、自动开关、1×/2×/4×、选择暂停自动、历史暂停/恢复、跳过取消、跳过确认在 Choice 停止、三个结局。停止 Play Mode 后确认场景没有新增未保存对象。

- [ ] **Step 6: 编写使用文档**

  `docs/DialogueSystem-Usage.md` 必须说明：创建 DialogueAsset、变量/条件/效果含义、三种节点字段、入口与跳转配置、校验按钮、Prefab 接入、Runner 启动 API、历史/自动/倍速/跳过行为、示例场景路径、运行测试方法和常见断链错误码。

- [ ] **Step 7: 最终 Console 与资源检查**

  调用 `read_console(action="get", types=["error"], count=100, format="detailed", include_stacktrace=true)`，预期零条 error。确认 Runtime 程序集没有 `UnityEditor` 引用，所有 Prefab/Scene 无 Missing Script，示例至少能到达两个不同结局。

- [ ] **Step 8: 建立最终检查点**

  若 Git 可用，提交：

  ```text
  test: verify dialogue system end to end
  ```

  若仍无 Git，保留完整测试结果、MCP 连接状态和视觉验收摘要，交付给主人。

---

## 执行顺序与暂停点

1. Task 1 完成 MCP 配置后，如 Codex 不能热加载工具，必须重启 Codex；这是唯一预期的流程暂停点。
2. Task 2–5 先完成可测试的剧情核心，不依赖最终 UI。
3. Task 6–7 完成交互状态机与 UGUI 视图。
4. Task 8 完成策划编辑体验。
5. Task 9 通过 MCP 生成并打开演示内容。
6. Task 10 只在前九项绿灯后进行整体验收。

## 完成定义

- MCP for Unity v10.0.0 已安装，Codex 可读取当前 Unity 场景与 Console。
- EditMode 与 PlayMode 全部测试通过。
- 示例剧情可到达 `authorized`、`wait`、`decline` 三个结局。
- 打字、自动、倍速、跳过、选项与已播放历史均按规格工作。
- 三种目标屏幕比例无关键布局缺陷。
- Unity Console 无编译错误，Runtime 不引用 UnityEditor。
- 使用文档可以让未参与实现的人从零创建并播放新的 DialogueAsset。
