# Dialogue System Plugin

一个面向 Unity UGUI 与 TextMeshPro 的可复用对话系统。它把对话数据、运行时流程与界面表现分开，支持线性台词、条件分支、变量效果、历史记录、自动播放、倍速、跳过，以及“对话结束后沿镜头路径进入下一段剧情”的导览模式。

> **English summary** — A reusable Unity dialogue framework built with UGUI and TextMeshPro. It supports branching dialogue, variables, history, auto/skip controls, Chinese text, and guided camera tours that hide the UI after the final line.

## 仓库定位 / Repository Contents

这个远端仓库同时保留两部分内容：

- 完整 Unity 开发与演示工程，包括 `Assets`、`Packages/manifest.json` 和 `ProjectSettings`，用于继续开发、测试与生成发布包。
- 可独立安装的 UPM 插件源码，位于 `Packages/com.zxxuh.dialogue-system`。

只想在其他 Unity 项目中使用插件时，不需要复制整个仓库，请直接使用下方 Git URL。需要参与开发或运行仓库级测试时，再克隆完整工程。`dist/*.tgz` 是本地可复现构建产物，不提交到远端。

## 功能一览 / Features

- `DialogueAsset` ScriptableObject 数据资产：线性台词、选项、结局、变量、条件与效果。
- 运行时 `DialogueSession`：纯 C# 状态机，负责节点推进、分支判断和历史记录。
- `DialogueRunner`：将会话状态发布为 Unity 事件，方便接入任意 UGUI 或游戏逻辑。
- `DialogueView`：逐字显示、点击补全/推进、选择按钮、`1X / 2X / 4X` 倍速、自动播放和跳过。
- “故事情节”历史面板：记录角色台词和玩家选择，支持滚动、自动定位到最新记录、上下边缘文字渐隐。
- `DialogueTourController`：按固定步骤淡出 UGUI、沿 Catmull-Rom 样条移动镜头、加载下一段对话；最后一步结束后彻底隐藏 UGUI。
- 随包 Noto Sans SC 中文字体与 TMP 多图集资产，不依赖操作系统字体。
- 3 套最小化中文 Demo，含线性剧情、分支、自动播放和镜头导览。

## 环境与依赖 / Requirements

- Unity `2022.3.62f1c1`
- UGUI `1.0.0`
- TextMeshPro `3.0.7`
- Unity Test Framework `1.1.33`

依赖版本记录在 [Packages/manifest.json](Packages/manifest.json)。首次打开工程时，请使用 Unity Hub 选择对应 LTS 编辑器版本并等待 Package Manager 完成导入。

## UPM 安装 / Installation

本仓库同时是 `com.zxxuh.dialogue-system@1.0.0` 的开发宿主。Git URL 中的 `path` 只安装仓库里的插件目录，不会把完整 Unity 工程复制到消费项目。请在 Package Manager 中选择 **Add package from git URL**，输入：

```text
https://github.com/OpenFrQuSh/Dialogue-System-.git?path=/Packages/com.zxxuh.dialogue-system#v1.0.0
```

仓库维护者也可以运行 `scripts/pack-upm.ps1` 生成本地归档。`dist` 已被 Git 忽略；如需测试 `.tgz`，请保留或复制该文件到消费项目可访问的位置，再在其 `Packages/manifest.json` 中添加相对路径依赖：

```text
file:../dist/com.zxxuh.dialogue-system-1.0.0.tgz
```

## 快速开始 / Quick Start

### 1. 导入并打开样例

在 Package Manager 中选择 **Dialogue System**，展开 **Samples**，导入 `Basic Dialogue` 或 `Guided Tours`。Unity 会将它们复制到 `Assets/Samples/Dialogue System/1.0.0/`：

| 场景 | 演示内容 |
| --- | --- |
| `Basic Dialogue/DialogueSystemSample.unity` | 基础分支对话、选择与历史 |
| `Guided Tours/01_AncientCityTour/AncientCityTour.unity` | 中文线性对话、逐字显示、三步骤镜头导览 |
| `Guided Tours/02_AbandonedLabTour/AbandonedLabTour.unity` | 中文分支、不同结局与历史记录 |
| `Guided Tours/03_RainyStreetTour/RainyStreetTour.unity` | 自动播放、倍速、跳过与最终界面隐藏 |

对话时可使用：

- 点击正文：补全当前逐字文本；再次点击进入下一句。
- `HISTORY`：打开或关闭“故事情节”面板。
- `AUTO`：开启/关闭自动推进；遇到选项会等待玩家选择。
- `1X`：循环切换 `1X / 2X / 4X`。
- `SKIP`：快速推进连续台词，在选择或结局处停下。

### 2. 重新生成样例

若需要恢复或更新自动生成的样例，请在 Unity 菜单执行：

```text
Tools > Dialogue System > Create Sample Scene
Tools > Dialogue System > Create Guided Tour Samples
```

第一个菜单生成基础 Sample；第二个菜单生成三套导览 Demo 与其 `Step01` 至 `Step03` 对话资产。导览生成器只覆盖自己拥有的固定样例文件，不会删除其他自定义文件。

生成结果统一写入 `Assets/DialogueSystemGenerated/Samples`。安装目录可能只读，因此生成器只读取包内字体，不会修改 `Packages/com.zxxuh.dialogue-system`。

### 3. 创建自己的 DialogueAsset

在 Project 窗口选择：

```text
Assets > Create > Dialogue System > Dialogue Asset
```

在 Inspector 中配置：

1. `Entry Node Id`：对话入口节点 ID。
2. `Variables`：Bool 或 Int 初始变量。
3. `Nodes`：由 `Line`、`Choice`、`End` 三种节点构成。
4. `Line`：填写 `Speaker`、`Text` 和 `Next Node Id`。
5. `Choice`：添加选项，每个选项可带条件、效果和目标节点。
6. `End`：填写稳定的 `Ending Id`，供游戏逻辑判断剧情结果。

`DialogueAsset` Inspector 会显示基础校验结果，例如入口不存在、重复 ID 或无效跳转。

### 4. 在自己的场景中启动对话

场景中准备一个 `DialogueRunner` 和一个已经绑定正文、角色名、选择列表与历史面板的 `DialogueView`，然后在启动时绑定并播放资产：

```csharp
using DialogueSystem.Data;
using DialogueSystem.Execution;
using DialogueSystem.UI;
using UnityEngine;

public sealed class MyDialogueStarter : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private DialogueAsset dialogueAsset;

    private void Start()
    {
        dialogueView.Bind(dialogueRunner);
        dialogueRunner.StartDialogue(dialogueAsset);
    }
}
```

最省心的方式是先生成 Sample，复制其中的 Canvas 与 `Dialogue Runtime` 配置，再替换为自己的 `DialogueAsset`。

## 框架结构 / Architecture

```text
DialogueAsset (ScriptableObject, dialogue graph and initial variables)
        |
        v
DialogueSession (pure C# state machine)
        |
        v
DialogueRunner (MonoBehaviour events: Presented / HistoryChanged / Ended / Failed)
        |
        +------------------------------+
        v                              v
DialogueView                    DialogueTourController
(UGUI presentation)            (step sequencing and camera movement)
        |                              |
        v                              v
History / Choices / TMP       Spline camera / Canvas fade-out
```

### 数据层：`DialogueAsset`

`DialogueAsset` 只存数据，不依赖场景。节点之间使用稳定 ID 跳转；变量定义、条件与效果也保存在资产中，因此同一套数据可被多个场景复用。

### 执行层：`DialogueSession` 与 `DialogueRunner`

`DialogueSession` 是不依赖 Unity UI 的状态机，负责过滤可见选项、应用变量效果、推进节点并写入历史。`DialogueRunner` 将状态转换为 Unity 事件：

- `Presented`：当前台词或选项需要显示。
- `HistoryChanged`：角色台词或玩家选择已写入历史。
- `Ended`：对话抵达 `End` 节点。
- `Failed`：数据无效或运行过程发生异常。

这使得你可以用自己的 UI、任务系统或存档系统订阅事件，而不需要改动对话核心。

### 表现层：`DialogueView`

`DialogueView` 订阅 Runner 并驱动 UGUI：逐字显示、按钮输入、自动模式和历史面板都位于这一层。历史面板标题为“故事情节”，以“人物名称 / 台词 / 分隔线”显示台词，以“你的选择”记录玩家决策；超出视口时可以滚动，文字靠近上下边界会平滑渐隐。

### 导览层：`DialogueTourController`

导览控制器将多个 `DialogueAsset` 串成固定步骤。每段结束后，流程为：

```text
结束当前对话
  -> UGUI 淡出且停止拦截输入
  -> 相机沿 Catmull-Rom 样条移动
  -> 在透明状态加载下一份 DialogueAsset
  -> UGUI 淡入
```

最后一步结束后，Canvas 会变为不可见、不可交互、不拦截射线，并被禁用，让玩家只观察场景。

## 中文字体 / Chinese Text

样例使用包内 [NotoSansSC-Variable.ttf](Packages/com.zxxuh.dialogue-system/Fonts/NotoSansSC-Variable.ttf) 与 TMP 字体资产 `NotoSansSC-Dynamic.asset`。`DialogueChineseFontProvider` 会把字体应用到 Canvas 下所有 `TMP_Text`，包括默认隐藏的历史面板和选择模板。

Noto Sans SC 使用 SIL Open Font License，许可证见 [Fonts/OFL.txt](Packages/com.zxxuh.dialogue-system/Fonts/OFL.txt)；完整声明见 [Third Party Notices.md](Packages/com.zxxuh.dialogue-system/Third%20Party%20Notices.md)。

## 目录结构 / Project Layout

```text
Packages/com.zxxuh.dialogue-system/
  Runtime/       # 数据、状态机、Runner 与 UGUI 运行时组件
  Editor/        # Inspector 校验器与安全 Sample 生成器
  Samples~/      # Package Manager 可导入的四套 Sample
  Tests/         # EditMode / PlayMode 自动化测试
  Fonts/         # Noto Sans SC 与 TMP 字体资产
Assets/DialogueSystemGenerated/  # 菜单生成的可编辑资源
Assets/                            # Unity 开发宿主资源
ProjectSettings/                   # Unity 开发宿主设置
scripts/         # UPM 发布脚本
docs/            # 样例与实现说明
```

## 测试 / Tests

在 Unity 中打开：

```text
Window > General > Test Runner
```

分别运行 `EditMode` 与 `PlayMode`。测试覆盖对话条件和效果、节点推进、跳过、逐字/自动播放、历史记录、样例生成、样条镜头、淡入淡出以及导览步骤切换。

## Git 提交说明 / Git Notes

项目的 [.gitignore](.gitignore) 已排除 Unity 与 IDE 生成物，例如 `Library/`、`Temp/`、`obj/`、`Logs/`、`UserSettings/`、`.vs/`、`*.csproj` 和 `*.sln`。提交时应保留 `Assets/`、`Packages/`、`ProjectSettings/`、`docs/` 及所有 Unity `.meta` 文件。

## License

本项目使用 [Apache License 2.0](LICENSE)。

---

## English Quick Guide

**What it is:** A Unity UGUI/TextMeshPro dialogue framework with branching nodes, variables, history, auto/skip controls, Chinese font support, and camera-guided dialogue tours.

**Install it:** Add `https://github.com/OpenFrQuSh/Dialogue-System-.git?path=/Packages/com.zxxuh.dialogue-system#v1.0.0` in Package Manager, then import either sample group.

**Try it:** Open an imported scene under `Assets/Samples/Dialogue System/1.0.0/` and enter Play Mode. Use `HISTORY`, `AUTO`, speed, and `SKIP` controls to explore the system.

**Create content:** Choose `Assets > Create > Dialogue System > Dialogue Asset`, configure an entry node, variables, line/choice/end nodes, then call:

```csharp
dialogueView.Bind(dialogueRunner);
dialogueRunner.StartDialogue(dialogueAsset);
```

**Generate demos:** Use `Tools > Dialogue System > Create Sample Scene` or `Create Guided Tour Samples`; generated assets stay under `Assets/DialogueSystemGenerated`.

**Tests:** Run EditMode and PlayMode suites from `Window > General > Test Runner`.
