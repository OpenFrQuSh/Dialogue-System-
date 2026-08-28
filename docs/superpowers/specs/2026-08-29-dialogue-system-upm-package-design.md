# Dialogue System UPM Package Design

## 目标

将当前 Unity `2022.3.62f1c1` 工程中的 `Assets/DialogueSystem` 转换为标准 Unity Package Manager 包。包 ID 固定为 `com.zxxuh.dialogue-system`，首个版本为 `1.0.0`。当前仓库继续作为开发与验证宿主，最终同时提供 Git URL 安装方式和本地 `.tgz` 发布包。

## 范围

本次迁移包含现有运行时代码、编辑器工具、中文字体、四套示例、EditMode 测试、PlayMode 测试、许可证和使用文档。迁移不改变对话运行时的公开行为，不重做界面，不删除用户资源，也不把工程级工具依赖（例如 MCP for Unity）加入插件依赖。

当前工作区中 `Assets/DialogueSystem/Fonts/NotoSansSC-Dynamic.asset` 的未提交内容随字体资产完整迁移；`ProjectSettings/EditorSettings.asset` 的未提交内容保持原位且不进入插件包。

## 仓库与包结构

插件作为当前 Unity 工程的嵌入式包维护：

```text
Packages/com.zxxuh.dialogue-system/
├─ package.json
├─ README.md
├─ CHANGELOG.md
├─ LICENSE.md
├─ Third Party Notices.md
├─ Runtime/
├─ Editor/
├─ Fonts/
├─ Samples~/
│  ├─ Basic Dialogue/
│  └─ Guided Tours/
└─ Tests/
   ├─ Editor/
   └─ Runtime/
```

`Runtime`、`Editor`、`Fonts` 和 `Tests` 从现有目录迁移，并保留对应 `.meta` 文件。测试目录按 Unity 2022.3 的包布局约定使用 `Tests/Editor` 和 `Tests/Runtime`，现有测试程序集名称保持不变。已有基础示例与三套导览示例迁入 `Samples~`，按基础示例和导览示例分组。Unity 不会为被忽略的 `Samples~` 内容自动生成 `.meta`，因此迁移时显式保留样例资源现有 `.meta`，并通过实际导入验证 GUID 和场景引用。

仓库根目录继续保留 `Assets`、`Packages`、`ProjectSettings`、项目 README 和开发文档，以便直接用 Unity 打开、运行测试和维护插件。用户通过以下形式的 Git URL 安装：

```text
https://github.com/OpenFrQuSh/Dialogue-System-.git?path=/Packages/com.zxxuh.dialogue-system#v1.0.0
```

该地址使用当前 `origin` 远端；发布 `v1.0.0` 标签后，包内安装说明使用此固定标签地址，开发说明可以另列不带标签的分支安装地址。

## 包元数据与依赖

`package.json` 使用以下稳定元数据：

- `name`: `com.zxxuh.dialogue-system`
- `displayName`: `Dialogue System`
- `version`: `1.0.0`
- `unity`: `2022.3`
- `description`: 说明分支对话、变量、历史、自动播放、跳过和导览能力

清单还包含两个 `samples` 条目，分别指向 `Samples~/Basic Dialogue` 和 `Samples~/Guided Tours`。许可证信息由包根目录的 `LICENSE.md` 提供，不添加 Unity 2022.3 包清单未定义的自定义许可证字段。

运行时只声明直接依赖：

- `com.unity.ugui`: `1.0.0`
- `com.unity.textmeshpro`: `3.0.7`

Unity Test Framework 仅用于开发和包测试，不作为最终用户运行时依赖。当前开发工程通过 manifest 的 `testables` 启用 `com.zxxuh.dialogue-system` 测试程序集，确保包从 Git 或 tarball 安装时也能显式加载测试。

## 程序集边界

`DialogueSystem.Runtime` 保持运行时程序集，只引用 `Unity.TextMeshPro` 与 `UnityEngine.UI`。`DialogueSystem.Editor` 仅在 Editor 平台编译，引用运行时程序集、TextMeshPro 和 UGUI。EditMode 与 PlayMode 测试分别保留独立测试程序集，且不会自动引用到用户运行时构建。

迁移后所有程序集名称和 C# 命名空间保持不变，避免破坏使用者代码和已序列化脚本引用。

## 示例与生成器行为

四套现有示例作为 UPM Samples 发布，用户在 Package Manager 的 Samples 页面按需导入。Samples 不在安装包加载时自动复制到 `Assets`，从而避免污染使用者项目。

现有菜单生成器继续保留，但所有生成输出统一写入：

```text
Assets/DialogueSystemGenerated/
```

生成器遵守以下约束：

- 不向 `Packages/com.zxxuh.dialogue-system` 或 Package Cache 写入文件。
- 只覆盖生成器拥有的固定场景、Prefab 和 DialogueAsset。
- 不删除 `Assets/DialogueSystemGenerated` 下的其他用户文件。
- 删除或覆盖前验证目标路径位于 `Assets/DialogueSystemGenerated` 内。
- 中文字体从 `Packages/com.zxxuh.dialogue-system/Fonts` 读取。
- 字体资源作为完成的预构建资产发布；生成器不重建或修改包内 TMP 字体图集。

包内资源定位使用稳定的虚拟 AssetDatabase 路径 `Packages/com.zxxuh.dialogue-system/...`。资源不存在时，编辑器工具抛出或记录包含包 ID、缺失路径和修复建议的明确错误。

## 字体和第三方许可证

Noto Sans SC 字体及动态 TMP 字体资产随包发布。`Third Party Notices.md` 明确列出字体名称、来源说明和 SIL Open Font License；原始 OFL 文件随字体保留。Apache License 2.0 作为插件自身许可证保存为 `LICENSE.md`。

## 文档迁移

包内 `README.md` 说明：

- 通过 Package Manager 使用 Git URL 或本地 tarball 安装。
- 依赖和 Unity 最低版本。
- 从 Samples 页面导入基础示例与导览示例。
- 创建 DialogueAsset、绑定 DialogueRunner 与 DialogueView。
- 使用菜单生成器以及新的生成目录。
- 启用并运行包测试。

仓库根 README 更新为开发仓库入口，并指向包内 README。现有文档中所有失效的 `Assets/DialogueSystem/...` 路径改为包路径、Samples 导入路径或生成输出路径，具体取决于资源用途。

## 打包交付

包的源目录为 `Packages/com.zxxuh.dialogue-system`。发布流程生成：

```text
dist/com.zxxuh.dialogue-system-1.0.0.tgz
```

打包过程必须可重复，且不得把开发工程的 `Library`、`Logs`、`obj`、`ProjectSettings`、MCP 配置或宿主工程测试输出收入 tarball。发布说明同时记录 Git URL 的 `?path=` 安装格式和本地 tarball 安装方式。

## 错误处理与安全边界

编辑器工具在开始生成前验证输出根目录、包内字体和必要资源。任何缺失资源、非法输出目录或 AssetDatabase 创建失败都应立即终止当前生成操作，并输出带上下文的错误。已经存在的用户自定义内容不作为清理目标。

迁移和打包不得重写当前未提交的 `ProjectSettings/EditorSettings.asset`。字体资产的当前未提交状态必须通过保留 `.meta` 和文件内容的移动操作延续到新路径。

## 验证策略

迁移完成后依次执行：

1. 校验包目录、`package.json`、许可证、程序集和依赖声明。
2. 搜索源码与文档，确认不存在仍应迁移的 `Assets/DialogueSystem` 硬编码路径。
3. 在开发工程中等待 Unity 完成重载，确认 Console 无编译错误。
4. 运行 `DialogueSystem.EditModeTests` 全量测试。
5. 运行 `DialogueSystem.PlayModeTests` 全量测试。
6. 从 Package Manager 导入 Samples，确认场景、DialogueAsset、Prefab 和字体 GUID 引用有效。
7. 运行两个示例生成菜单，确认只在 `Assets/DialogueSystemGenerated` 写入并可重复执行。
8. 在临时最小 Unity `2022.3` 工程中安装本地包，确认依赖能由 `package.json` 独立解析且无额外工程依赖。
9. 生成 `.tgz`，检查归档内容并用该归档完成一次本地安装烟雾测试。

若当前环境无法自动驱动 Unity，则必须完成静态包检查和归档检查，并明确报告尚未执行的 Unity 导入、编译或测试步骤；不得将未运行的验证描述为通过。

## 验收标准

- `com.zxxuh.dialogue-system` 以版本 `1.0.0` 被 Unity Package Manager 识别。
- Git `?path=` 和本地 `.tgz` 两种安装方式具备完整交付物与说明。
- Runtime 与 Editor 在最小依赖工程中零编译错误。
- EditMode 与 PlayMode 测试通过。
- 四套 Samples 可按需导入，场景和字体引用保持有效。
- 菜单生成器不写入只读包目录，不覆盖用户自定义资源。
- Noto Sans SC 的许可证和插件 Apache 2.0 许可证完整随包发布。
- 主人现有未提交修改得到保留，且无无关文件进入迁移提交。
