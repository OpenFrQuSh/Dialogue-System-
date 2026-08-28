# 分步骤镜头导览对话样例设计规格

## 1. 目标

在现有 `Assets/DialogueSystem/` 插件之上新增三套可独立运行的最小化 Demo，重点演示中文对话、分步骤剧情推进、选项分支、历史记录、自动播放、倍速、跳过，以及对话完成后关闭 UGUI 的沉浸式收尾。

场景只作为对话系统的承载环境，不制作建筑、模型、材质或环境装饰。镜头仍按隐藏的路径控制点移动，用来演示游戏流程如何在一段对话完成后进入下一个步骤。

## 2. 范围

新增三套样例目录：

```text
Assets/DialogueSystem/Samples/
  01_AncientCityTour/
  02_AbandonedLabTour/
  03_RainyStreetTour/
```

每套目录包含：

- 一份独立 Unity 场景。
- 三个步骤使用的中文 `DialogueAsset`。
- 一条由空 GameObject 控制点定义的镜头路径。
- 一套可直接运行的对话 UGUI。
- 必要的 Prefab 或配置资源。

不新增第三方运行时依赖，不引入外部模型、纹理、音频或环境资源，不修改对话数据模型的既有语义。

## 3. 三套 Demo 的演示重点

### 3.1 01_AncientCityTour

- 中文线性剧情。
- 逐字显示与点击补全。
- 三个观察步骤顺序推进。
- 最后一段结束后淡出并隐藏 UGUI。

### 3.2 02_AbandonedLabTour

- 中文选择分支。
- 选择记录写入历史面板。
- 不同选项可进入不同台词或结局，但步骤总数保持为三步。
- 分支结束后继续进入下一镜头步骤，而不是提前销毁导览控制器。

### 3.3 03_RainyStreetTour

- 自动播放、`1X / 2X / 4X` 倍速和跳过功能。
- 自动播放在选择节点等待玩家输入。
- 对话步骤与镜头路径协同。
- 最终步骤验证 UGUI 的完整关闭状态。

三套 Demo 都使用最小场景结构；主题只体现在中文示例文本中，不通过场景美术表现。

## 4. 运行时架构

### 4.1 DialogueTourController

`DialogueTourController` 是样例流程协调器，持有有序步骤列表。每个步骤包含：

- 本步骤的 `DialogueAsset`。
- 镜头路径上的目标参数或目标控制点索引。
- 镜头移动时长。
- 到达后的可选停顿时间。

控制器订阅 `DialogueRunner.Ended` 和 `DialogueRunner.Failed`。正常流程为：

1. 首个步骤直接在首个观察点开始对话。
2. 当前 `DialogueAsset` 发布 `Ended`。
3. 淡出 UGUI 并暂时禁止交互。
4. 沿样条移动至下一观察点。
5. 镜头到位后，在 UGUI 仍为透明状态时加载下一步骤文本。
6. 使用同一个 `DialogueRunner` 启动下一步骤的 `DialogueAsset`，随后淡入 UGUI。
7. 最终步骤结束后执行永久关闭，不再启动新对话。

控制器在切换期间忽略重复结束通知，避免双击、跳过或重复事件导致跨越多个步骤。

### 4.2 DialogueCameraSpline

镜头路径使用 Catmull-Rom 曲线。场景中的控制点是没有 Renderer 的空 GameObject，运行时不可见；编辑器通过 Gizmos 绘制控制点和采样曲线。

移动期间同时插值位置和观察方向，并使用缓入缓出曲线避免起停突兀。组件不依赖 Cinemachine 或 Timeline。路径点不足时采用以下降级策略：

- 零个点：记录错误并阻止导览启动。
- 一个点：相机直接定位。
- 两个点：使用线性插值。
- 三个及以上：使用 Catmull-Rom 曲线。

### 4.3 DialogueCanvasFader

组件通过 `CanvasGroup` 管理对话界面：

- 淡入前启用 Canvas 对象，完成后允许交互和射线。
- 淡出开始时立即禁止交互和射线。
- 中间步骤淡出后保持对象激活，供下一步骤复用。
- 最终淡出完成后把对话 Canvas 设为不可见并禁用对象。

最终状态必须满足：`alpha = 0`、`interactable = false`、`blocksRaycasts = false`、Canvas 根对象 inactive。

### 4.4 DialogueChineseFontProvider

当前工程只有 `LiberationSans.ttf`，不包含完整中文字形。样例随包加入 SIL Open Font License 授权的 Noto Sans SC 字体文件，并由编辑器生成器创建 Dynamic、Multi Atlas 的 TMP 字体资产。

`DialogueChineseFontProvider` 在 `Awake()` 中把随包字体应用到样例 Canvas 下所有 `TMP_Text`，包括 inactive 的历史面板与选择模板。字体缺失时保留默认字体并输出一次明确警告；不得每帧重复创建字体或刷屏日志。

随包字体消除操作系统字体差异，保证当前 Windows 编辑器、Windows 构建以及其他能够运行该 Unity 项目的平台使用同一份中文字形。字体来源与 `OFL.txt` 必须和字体文件一起保留。

## 5. 场景结构

每个场景只包含以下必要对象：

```text
Demo Root
  Main Camera
  Directional Light
  EventSystem
  Camera Path
    Point 00
    Point 01
    Point 02
    Point 03
  Dialogue Runtime
    Dialogue Runner
    Dialogue Tour Controller
    Dialogue View
  Dialogue Canvas
```

场景不创建建筑或装饰几何体。相机背景色可以区分三套 Demo，但不生成额外视觉资产。所有路径点只在 Scene 视图以 Gizmos 显示。

## 6. 数据流与状态

导览控制器使用以下互斥状态：

```text
Initializing -> Presenting -> FadingOut -> Moving -> FadingIn -> Presenting
                                  |
                                  +-> Completed（最终步骤）
```

- `Presenting` 状态才接受对话结束事件。
- `FadingOut`、`Moving`、`FadingIn` 状态期间禁止对话 UI 输入。
- 选择分支只由既有 `DialogueRunner` 处理，导览控制器只关心整个资产的 `Ended`。
- `Failed` 事件使导览停止、关闭交互并保留场景，Console 输出当前 Demo 名与步骤索引。
- 组件销毁时必须退订 Runner 事件，防止重新加载场景后残留回调。

## 7. 编辑器生成器

扩展现有 `DialogueSampleBuilder`，新增单独菜单命令生成三套导览样例。生成器必须幂等：重复执行会更新固定路径下的生成资源，不产生带编号的重复副本。

生成器负责：

- 创建三套目录和中文对话资产。
- 创建最小场景层级、路径点、组件引用和 Canvas。
- 设置 TMP 默认占位字体；运行时再由中文字体提供器替换。
- 保存场景和资源并调用 `AssetDatabase.SaveAssets()`。
- 不删除 `Samples` 下用户自行创建的其他文件或目录。

现有 `DialogueSystemSample` 保留不动，三套新 Demo 作为追加内容。

## 8. 测试与验收

### 8.1 EditMode

- Catmull-Rom 端点、区间和两点降级计算正确。
- 步骤列表为空、路径为空时返回明确错误。
- 中文字体候选选择顺序稳定，找不到字体时只报告一次警告。

### 8.2 PlayMode

- 当前步骤结束后只推进一个步骤。
- 切换期间的重复 `Ended` 不会跳步。
- 镜头移动前 UI 禁止交互，移动结束后恢复。
- 最终步骤结束后 Canvas 根对象 inactive。
- 分支 Demo 选择后能够进入正确结局并继续下一步骤。
- 自动播放遇到选择节点不会自行选择。

### 8.3 MCP 端到端验收

- Unity MCP 连接到 `Dialogue System Plugin@ba577180dcd0ef0c` 或当前同名有效实例。
- 执行样例生成菜单后，三套目录、场景和对话资产存在。
- 依次打开三套场景，场景无 Missing Script，Console 无编译错误。
- 运行每套场景，确认中文可见、步骤顺序正确、镜头按路径移动。
- 最后一段对话完成后，UGUI 淡出且不再阻挡场景输入。
- 全部 EditMode 与 PlayMode 测试通过。

## 9. 完成定义

- 三套最小化 Demo 可独立进入 Play Mode 演示。
- 每套都有三个对话步骤和一条隐藏的镜头路径。
- 中文在当前 Windows 编辑器与 Windows 构建目标中正常显示。
- 既有逐字、选择、历史、自动、倍速和跳过功能在对应样例中可体验。
- 中间步骤按“结束对话、淡出、移动、淡入、开始下一段”执行。
- 最终步骤完成后整个 UGUI 不可见、不可交互、不可拦截射线。
- 不引入场景美术内容或新的第三方运行时依赖。
