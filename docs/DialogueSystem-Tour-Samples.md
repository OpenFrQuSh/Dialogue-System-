# 分步骤对话导览样例

## 样例位置

- `Assets/DialogueSystem/Samples/01_AncientCityTour/AncientCityTour.unity`
- `Assets/DialogueSystem/Samples/02_AbandonedLabTour/AbandonedLabTour.unity`
- `Assets/DialogueSystem/Samples/03_RainyStreetTour/RainyStreetTour.unity`

三套场景都是最小空场景，只包含相机、Directional Light、EventSystem、隐藏路径点、对话运行对象和 UGUI。主题名称只用于区分中文示例文本，不包含建筑、模型或环境素材。

历史按钮会打开标题为“故事情节”的纵向记录面板。人物台词按“人物名称、台词、分隔线”排列，玩家做过的选项以“你的选择”记录；内容超出视口后可上下滚动，打开时自动定位到最新记录，文字靠近上下边界时逐渐透明。

## 三套 Demo 的重点

`01_AncientCityTour` 演示中文线性对话、逐字显示、点击补全、三步骤导览与最终关闭。

`02_AbandonedLabTour` 演示中文选项分支、不同结局与历史记录。每个步骤都包含两个选择，完成当前分支后才进入下一镜头步骤。

`03_RainyStreetTour` 演示自动播放、`1X / 2X / 4X` 倍速和跳过。自动播放遇到选择时仍会等待玩家决定。

## 运行流程

每个步骤按照以下顺序执行：

```text
播放 DialogueAsset
  -> DialogueRunner.Ended
  -> UGUI 淡出并禁止交互
  -> 相机沿 Catmull-Rom 路径移动
  -> 在透明状态下加载下一份 DialogueAsset
  -> UGUI 淡入并恢复交互
```

最后一步结束后，`DialogueCanvasFader` 将 CanvasGroup 设为不可见、不可交互、不拦截射线，并禁用 `Dialogue Canvas` 根对象。相机保持在最后一个观察点。

## 修改步骤和路径

`Dialogue Runtime` 上的 `DialogueTourController` 保存有序步骤。每个 `DialogueTourStep` 包含：

- `Dialogue`：当前步骤使用的 DialogueAsset。
- `Path Point Index`：目标路径点索引。
- `Move Duration`：镜头移动秒数，使用未缩放时间。
- `Arrival Delay`：到达后开始淡入前的停顿。

`Camera Path` 下的 `Point 00` 至 `Point 03` 是空 GameObject。选择 `Dialogue Runtime` 上的 `DialogueCameraSpline` 时，Scene 视图会显示控制点和采样曲线；这些 Gizmos 不会进入 Game 画面。

前三个路径点对应三个观察步骤，第四个点只用于稳定末段曲线切线。增加步骤时，应同时增加有效路径点并更新步骤索引。

## 中文字体

样例随包使用 `Assets/DialogueSystem/Fonts/NotoSansSC-Variable.ttf`，并由生成器创建动态、多图集 TMP 字体：

```text
Assets/DialogueSystem/Fonts/NotoSansSC-Dynamic.asset
```

字体来自 Google Fonts 的 Noto Sans SC，使用 SIL Open Font License；许可证保存在同目录的 `OFL.txt`。生成器会预热三套 Demo 实际使用的中文，并把 TMP 材质、字符表和 Atlas 一起保存到字体资产；运行时仍可按需扩充字符，不会预生成全部 CJK 字形。

`DialogueChineseFontProvider.Awake()` 会在对话控制器的 `Start()` 前，把该字体应用到 Canvas 下所有 TMP_Text，包括尚未激活的历史面板和选择模板。

## 重新生成

在 Unity 菜单执行：

```text
Tools -> Dialogue System -> Create Guided Tour Samples
```

生成器只覆盖三套固定目录中的同名场景、`Step01.asset`、`Step02.asset` 和 `Step03.asset`，不会删除目录中的其他自定义文件，也不会删除原始 `DialogueSystemSample`。

重新生成前应确保 `NotoSansSC-Variable.ttf` 与 `OFL.txt` 存在。生成完成后，三套场景会在缺失时追加到 Build Settings，不会移除已有场景。

## 复用到游戏

在正式游戏场景中复用时：

1. 保留既有 `DialogueRunner` 与 `DialogueView`。
2. 在 Canvas 根对象添加 `CanvasGroup`、`DialogueCanvasFader` 与 `DialogueChineseFontProvider`。
3. 创建路径点并配置 `DialogueCameraSpline`。
4. 在 `DialogueTourController` 中按游戏步骤配置 DialogueAsset 和路径点索引。
5. 若由其他游戏系统决定启动时机，关闭 `playOnStart`，再调用 `BeginTour()`。

导览控制器只订阅 `DialogueRunner.Ended` 与 `Failed`，不会接管对话内部的条件、变量、选项或结局逻辑。
