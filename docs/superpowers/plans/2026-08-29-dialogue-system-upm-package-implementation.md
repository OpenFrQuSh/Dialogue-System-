# Dialogue System UPM Package Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将现有 `Assets/DialogueSystem` 迁移为可通过 Git URL 和本地 tarball 安装的 `com.zxxuh.dialogue-system@1.0.0` 标准 UPM 包。

**Architecture:** 当前 Unity 工程继续作为开发宿主，插件源码作为嵌入式包存放于 `Packages/com.zxxuh.dialogue-system`。运行时、编辑器、字体、测试和 Samples 各自保持清晰边界；编辑器生成器只通过集中路径契约读取包内资源，并只向 `Assets/DialogueSystemGenerated` 写入。根目录 PowerShell 发布脚本校验包内容后使用 `npm pack` 生成 Unity 可安装的 `.tgz`。

**Tech Stack:** Unity `2022.3.62f1c1`、C#、UGUI `1.0.0`、TextMeshPro `3.0.7`、Unity Test Framework `1.1.33`、PowerShell 7、Node.js/npm、Git。

**Spec:** `docs/superpowers/specs/2026-08-29-dialogue-system-upm-package-design.md`

## Global Constraints

- 包 ID 固定为 `com.zxxuh.dialogue-system`，首个版本固定为 `1.0.0`。
- Unity 最低版本固定为 `2022.3`；开发宿主版本为 `2022.3.62f1c1`。
- 运行时直接依赖只允许 `com.unity.ugui@1.0.0` 和 `com.unity.textmeshpro@3.0.7`。
- `DialogueSystem.Runtime`、`DialogueSystem.Editor` 及现有命名空间不得重命名。
- 菜单生成器只能写入或删除 `Assets/DialogueSystemGenerated` 内的资源。
- `Assets/DialogueSystem/Fonts/NotoSansSC-Dynamic.asset` 当前未提交内容必须随文件迁移并保留。
- `ProjectSettings/EditorSettings.asset` 当前未提交内容不得修改、暂存或提交。
- 历史规格和历史实施计划作为记录保留；只更新根 README、包 README 与当前用户文档中的安装和资源路径。
- 新增或改写的 C# 逻辑、AssetDatabase 调用、空值保护和路径安全分支上方必须写明原因与意图。
- Unity Editor 当前未在 PATH、常见 Hub 目录或运行进程中发现；无法执行的 Unity 编译或 Test Runner 步骤必须明确标记为未运行，不能描述为通过。

## Target File Map

```text
Packages/com.zxxuh.dialogue-system/
  package.json                         # UPM 元数据、依赖和 Samples 清单
  README.md                            # 安装与使用文档
  CHANGELOG.md                         # 1.0.0 发布记录
  LICENSE.md                           # Apache-2.0
  Third Party Notices.md               # Noto Sans SC 第三方声明
  Runtime/                             # 原运行时程序集与资源
  Editor/
    DialoguePackagePaths.cs            # 包路径、生成路径和安全删除边界
    DialogueSampleBuilder.cs           # 基础示例生成到 Assets
    DialogueTourSampleBuilder.cs       # 导览示例生成到 Assets
  Fonts/                               # 预构建 TMP 字体、源字体和 OFL
  Samples~/
    Basic Dialogue/                    # 基础场景、DialogueAsset、Prefab
    Guided Tours/                      # 三套导览场景与 DialogueAsset
  Tests/
    Editor/                            # 原 EditMode 测试和路径安全测试
    Runtime/                           # 原 PlayMode 测试
scripts/
  pack-upm.ps1                         # 静态校验并生成 tgz
dist/
  com.zxxuh.dialogue-system-1.0.0.tgz # 发布产物，不提交 Git
```

---

### Task 1: Create the embedded package and migrate code, fonts, and tests

**Files:**
- Create: `Packages/com.zxxuh.dialogue-system/package.json`
- Move: `Assets/DialogueSystem/Runtime` → `Packages/com.zxxuh.dialogue-system/Runtime`
- Move: `Assets/DialogueSystem/Editor` → `Packages/com.zxxuh.dialogue-system/Editor`
- Move: `Assets/DialogueSystem/Fonts` → `Packages/com.zxxuh.dialogue-system/Fonts`
- Move: `Assets/DialogueSystem/Tests/EditMode` → `Packages/com.zxxuh.dialogue-system/Tests/Editor`
- Move: `Assets/DialogueSystem/Tests/PlayMode` → `Packages/com.zxxuh.dialogue-system/Tests/Runtime`
- Modify: `Packages/manifest.json`
- Verify unchanged: `ProjectSettings/EditorSettings.asset`

**Interfaces:**
- Consumes: existing assemblies `DialogueSystem.Runtime`, `DialogueSystem.Editor`, `DialogueSystem.EditModeTests`, and `DialogueSystem.PlayModeTests`.
- Produces: embedded package `com.zxxuh.dialogue-system@1.0.0` with unchanged assembly names and `Packages/manifest.json.testables` containing the package ID.

- [ ] **Step 1: Record the user-owned dirty state before moving files**

Run:

```powershell
git status --short
git hash-object -- "Assets/DialogueSystem/Fonts/NotoSansSC-Dynamic.asset"
git hash-object -- "ProjectSettings/EditorSettings.asset"
```

Expected: both files are modified; record both hashes. The second hash must remain identical through all implementation tasks.

- [ ] **Step 2: Verify the package shell does not exist yet**

Run:

```powershell
Test-Path -LiteralPath "Packages/com.zxxuh.dialogue-system/package.json"
```

Expected: `False`.

- [ ] **Step 3: Create the package manifest**

Create `Packages/com.zxxuh.dialogue-system/package.json` with `apply_patch`:

```json
{
  "name": "com.zxxuh.dialogue-system",
  "displayName": "Dialogue System",
  "version": "1.0.0",
  "unity": "2022.3",
  "description": "A reusable UGUI and TextMeshPro dialogue system with branching, variables, history, auto play, skip controls, and guided camera tours.",
  "keywords": [
    "dialogue",
    "narrative",
    "ugui",
    "textmeshpro"
  ],
  "author": {
    "name": "OpenFrQuSh"
  },
  "dependencies": {
    "com.unity.ugui": "1.0.0",
    "com.unity.textmeshpro": "3.0.7"
  },
  "samples": [
    {
      "displayName": "Basic Dialogue",
      "description": "A branching dialogue scene with choices and history.",
      "path": "Samples~/Basic Dialogue"
    },
    {
      "displayName": "Guided Tours",
      "description": "Three Chinese dialogue tours demonstrating branching, auto play, skip, and camera movement.",
      "path": "Samples~/Guided Tours"
    }
  ]
}
```

- [ ] **Step 4: Move existing assets with their `.meta` files**

Resolve and inspect the exact roots before moving:

```powershell
Resolve-Path -LiteralPath "Assets/DialogueSystem"
Resolve-Path -LiteralPath "Packages"
```

Both resolved paths must be children of `D:\demo\Dialogue System Plugin`. Then create the package root and use `Move-Item -LiteralPath` for `Runtime`, `Editor`, `Fonts`, and `Tests`; move each sibling folder meta file with it. Inside package `Tests`, rename `EditMode` plus `EditMode.meta` to `Editor` plus `Editor.meta`, and rename `PlayMode` plus `PlayMode.meta` to `Runtime` plus `Runtime.meta`.

Use this exact PowerShell move sequence after the checks:

```powershell
$workspaceRoot = (Resolve-Path -LiteralPath ".").Path
$packageRoot = (Resolve-Path -LiteralPath "Packages/com.zxxuh.dialogue-system").Path
if (-not $packageRoot.StartsWith($workspaceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Package root escaped the workspace: $packageRoot"
}

foreach ($name in @("Runtime", "Editor", "Fonts", "Tests")) {
    Move-Item -LiteralPath "Assets/DialogueSystem/$name" -Destination "Packages/com.zxxuh.dialogue-system/$name"
    Move-Item -LiteralPath "Assets/DialogueSystem/$name.meta" -Destination "Packages/com.zxxuh.dialogue-system/$name.meta"
}

Move-Item -LiteralPath "Packages/com.zxxuh.dialogue-system/Tests/EditMode" -Destination "Packages/com.zxxuh.dialogue-system/Tests/Editor"
Move-Item -LiteralPath "Packages/com.zxxuh.dialogue-system/Tests/EditMode.meta" -Destination "Packages/com.zxxuh.dialogue-system/Tests/Editor.meta"
Move-Item -LiteralPath "Packages/com.zxxuh.dialogue-system/Tests/PlayMode" -Destination "Packages/com.zxxuh.dialogue-system/Tests/Runtime"
Move-Item -LiteralPath "Packages/com.zxxuh.dialogue-system/Tests/PlayMode.meta" -Destination "Packages/com.zxxuh.dialogue-system/Tests/Runtime.meta"
```

Do not move `Samples` in this step. Do not touch `ProjectSettings/EditorSettings.asset`.

- [ ] **Step 5: Enable package tests in the host project**

Modify the end of `Packages/manifest.json` with `apply_patch` so the top-level object contains:

```json
  "testables": [
    "com.zxxuh.dialogue-system"
  ]
```

Keep the existing `dependencies` object byte-for-byte except for the comma needed before `testables`.

- [ ] **Step 6: Validate the package manifest and migrated structure**

Run:

```powershell
$package = Get-Content -LiteralPath "Packages/com.zxxuh.dialogue-system/package.json" -Raw | ConvertFrom-Json
$project = Get-Content -LiteralPath "Packages/manifest.json" -Raw | ConvertFrom-Json
$package.name
$package.version
$package.dependencies."com.unity.ugui"
$package.dependencies."com.unity.textmeshpro"
$project.testables
Test-Path -LiteralPath "Packages/com.zxxuh.dialogue-system/Runtime/DialogueSystem.Runtime.asmdef"
Test-Path -LiteralPath "Packages/com.zxxuh.dialogue-system/Editor/DialogueSystem.Editor.asmdef"
Test-Path -LiteralPath "Packages/com.zxxuh.dialogue-system/Tests/Editor/DialogueSystem.EditModeTests.asmdef"
Test-Path -LiteralPath "Packages/com.zxxuh.dialogue-system/Tests/Runtime/DialogueSystem.PlayModeTests.asmdef"
```

Expected: IDs and versions match the global constraints; all four path checks return `True`.

- [ ] **Step 7: Verify user modifications were preserved**

Run the two `git hash-object` commands from Step 1 again. Expected: both hashes exactly match the recorded values. `git status --short` must still show `ProjectSettings/EditorSettings.asset` modified and unstaged.

- [ ] **Step 8: Commit the package skeleton and mechanical moves**

Stage only `Packages/com.zxxuh.dialogue-system`, `Packages/manifest.json`, and deleted/moved paths under `Assets/DialogueSystem/Runtime`, `Editor`, `Fonts`, and `Tests`. Confirm `ProjectSettings/EditorSettings.asset` is not staged, then commit:

```powershell
git commit -m "feat: embed dialogue system UPM package"
```

---

### Task 2: Add a safe package path contract and refactor generators

**Files:**
- Create: `Packages/com.zxxuh.dialogue-system/Editor/DialoguePackagePaths.cs`
- Create: `Packages/com.zxxuh.dialogue-system/Editor/DialoguePackagePaths.cs.meta`
- Create: `Packages/com.zxxuh.dialogue-system/Tests/Editor/DialoguePackagePathsTests.cs`
- Create: `Packages/com.zxxuh.dialogue-system/Tests/Editor/DialoguePackagePathsTests.cs.meta`
- Modify: `Packages/com.zxxuh.dialogue-system/Editor/DialogueSampleBuilder.cs`
- Modify: `Packages/com.zxxuh.dialogue-system/Editor/DialogueTourSampleBuilder.cs`
- Modify: `Packages/com.zxxuh.dialogue-system/Tests/Editor/DialogueChineseFontProviderTests.cs`
- Modify: `Packages/com.zxxuh.dialogue-system/Tests/Editor/DialogueTourSampleBuilderTests.cs`

**Interfaces:**
- Consumes: Unity `AssetDatabase`, package ID `com.zxxuh.dialogue-system`, existing sample builders.
- Produces: `DialoguePackagePaths.PackageRoot`, `BundledFontSourcePath`, `BundledFontAssetPath`, `GeneratedRoot`, `GeneratedSamplesRoot`, `IsGeneratedAssetPath(string)`, `EnsureGeneratedFolder(string)`, and `DeleteGeneratedAsset(string)`.

- [ ] **Step 1: Write failing path-boundary tests**

Create `DialoguePackagePathsTests.cs`:

```csharp
using System;
using DialogueSystem.Editor;
using NUnit.Framework;

namespace DialogueSystem.Tests
{
    public sealed class DialoguePackagePathsTests
    {
        [TestCase("Assets/DialogueSystemGenerated", true)]
        [TestCase("Assets/DialogueSystemGenerated/Samples/Test.asset", true)]
        [TestCase("Assets/DialogueSystemGeneratedElsewhere/Test.asset", false)]
        [TestCase("Assets/UserContent/Test.asset", false)]
        [TestCase("Packages/com.zxxuh.dialogue-system/Fonts/NotoSansSC-Dynamic.asset", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsGeneratedAssetPath_OnlyAcceptsOwnedRoot(string path, bool expected)
        {
            Assert.That(DialoguePackagePaths.IsGeneratedAssetPath(path), Is.EqualTo(expected));
        }

        [Test]
        public void DeleteGeneratedAsset_RejectsUserContent()
        {
            Assert.That(
                () => DialoguePackagePaths.DeleteGeneratedAsset("Assets/UserContent/Test.asset"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("com.zxxuh.dialogue-system"));
        }
    }
}
```

- [ ] **Step 2: Attempt the focused Editor test and record the red state**

The current machine has no discoverable Unity executable, so first run:

```powershell
Get-Command Unity,Unity.exe -ErrorAction SilentlyContinue
Get-Process -Name Unity -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Path
```

Current expected result: no path is returned. Record `DialoguePackagePathsTests` as not runnable in the current shell; do not claim a red Test Runner result. Static expected compile failure is that `DialoguePackagePaths` does not exist.

- [ ] **Step 3: Implement the centralized path contract**

Create `DialoguePackagePaths.cs` with this behavior:

```csharp
using System;
using UnityEditor;

namespace DialogueSystem.Editor
{
    public static class DialoguePackagePaths
    {
        public const string PackageId = "com.zxxuh.dialogue-system";
        public const string PackageRoot = "Packages/" + PackageId;
        public const string BundledFontSourcePath = PackageRoot + "/Fonts/NotoSansSC-Variable.ttf";
        public const string BundledFontAssetPath = PackageRoot + "/Fonts/NotoSansSC-Dynamic.asset";
        public const string GeneratedRoot = "Assets/DialogueSystemGenerated";
        public const string GeneratedSamplesRoot = GeneratedRoot + "/Samples";

        public static bool IsGeneratedAssetPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath)
                   && (string.Equals(assetPath, GeneratedRoot, StringComparison.Ordinal)
                       || assetPath.StartsWith(GeneratedRoot + "/", StringComparison.Ordinal));
        }

        public static void EnsureGeneratedFolder(string folderPath)
        {
            RequireGeneratedPath(folderPath);
            var parts = folderPath.Split('/');
            var parent = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var current = parent + "/" + parts[index];
                // AssetDatabase 只能逐层创建目录；逐段验证也能保证生成器不会越出自有根目录。
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, parts[index]);
                }

                parent = current;
            }
        }

        public static void DeleteGeneratedAsset(string assetPath)
        {
            RequireGeneratedPath(assetPath);
            // 仅删除生成器拥有的固定资源，避免菜单操作误伤主人自己的 Assets 内容。
            AssetDatabase.DeleteAsset(assetPath);
        }

        private static void RequireGeneratedPath(string assetPath)
        {
            if (!IsGeneratedAssetPath(assetPath))
            {
                throw new InvalidOperationException(
                    $"[{PackageId}] 拒绝写入或删除非生成目录资源：{assetPath}。请使用 {GeneratedRoot}。 ");
            }
        }
    }
}
```

Create deterministic Unity metadata with `apply_patch`:

```yaml
# DialoguePackagePaths.cs.meta
fileFormatVersion: 2
guid: 8aa39c4e6b5b4d7d8794bb66b5a04721
```

```yaml
# DialoguePackagePathsTests.cs.meta
fileFormatVersion: 2
guid: c7195fc8ac8e40efaa6b726d3ab76c1e
```

- [ ] **Step 4: Refactor the basic sample builder**

Replace the three `Assets/DialogueSystem/Samples` constants with:

```csharp
var scenePath = DialoguePackagePaths.GeneratedSamplesRoot + "/DialogueSystemSample.unity";
var assetPath = DialoguePackagePaths.GeneratedSamplesRoot + "/DialogueSystemSample.asset";
var prefabPath = DialoguePackagePaths.GeneratedSamplesRoot + "/DialogueSystemCanvas.prefab";
DialoguePackagePaths.DeleteGeneratedAsset(scenePath);
DialoguePackagePaths.DeleteGeneratedAsset(assetPath);
DialoguePackagePaths.DeleteGeneratedAsset(prefabPath);
DialoguePackagePaths.EnsureGeneratedFolder(DialoguePackagePaths.GeneratedSamplesRoot);
```

Load the font from `DialoguePackagePaths.BundledFontAssetPath`. Add a null guard that throws `InvalidOperationException` containing the package ID and missing path. Remove the old one-line `EnsureFolder` method.

- [ ] **Step 5: Refactor the guided-tour builder without mutating package assets**

Set `SamplesRoot` from `DialoguePackagePaths.GeneratedSamplesRoot`, use `EnsureGeneratedFolder` for owned directories, and replace both direct generated-file `AssetDatabase.DeleteAsset` calls with `DeleteGeneratedAsset`.

Replace `EnsureBundledChineseFontAsset` with a read-only loader:

```csharp
private static TMP_FontAsset LoadBundledChineseFontAsset()
{
    var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
        DialoguePackagePaths.BundledFontAssetPath);
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
    if (!fontAsset.HasCharacters(requiredCharacters))
    {
        throw new InvalidOperationException(
            $"[{DialoguePackagePaths.PackageId}] 随包字体缺少 Demo 所需中文字符。 ");
    }

    return fontAsset;
}
```

Delete `PersistFontSubAssets`, remove the now-unused `UnityEngine.TextCore.LowLevel` import, and keep `System.Text` because `CollectRequiredFontCharacters` still uses it.

- [ ] **Step 6: Update existing tests to consume the shared contract**

Change the bundled font test to load `DialoguePackagePaths.BundledFontAssetPath`. Change tour-builder tests to open and assert paths under `DialoguePackagePaths.GeneratedSamplesRoot`.

Replace the old “original sample still exists” assertion with a user-content sentinel created under `DialoguePackagePaths.GeneratedRoot`; call `BuildAll`, assert the sentinel still exists, then delete only that exact test sentinel in a `finally` block.

- [ ] **Step 7: Run static path checks and focused tests when possible**

Run:

```powershell
rg -n "Assets/DialogueSystem/(Samples|Fonts)" "Packages/com.zxxuh.dialogue-system/Editor" "Packages/com.zxxuh.dialogue-system/Tests"
rg -n "AssetDatabase.DeleteAsset" "Packages/com.zxxuh.dialogue-system/Editor"
```

Expected: first command returns no matches. Second command returns only the guarded call inside `DialoguePackagePaths.DeleteGeneratedAsset`.

If Unity becomes discoverable, run `DialogueSystem.Tests.DialoguePackagePathsTests`, `DialogueSystem.Tests.DialogueChineseFontProviderTests`, and `DialogueSystem.Tests.DialogueTourSampleBuilderTests`; expected: all pass. Otherwise record them as not run.

- [ ] **Step 8: Commit the path-safety refactor**

```powershell
git commit -m "fix: make dialogue generators package-safe"
```

---

### Task 3: Convert shipped demos into UPM Samples

**Files:**
- Move: `Assets/DialogueSystem/Samples/DialogueSystemSample.*` → `Packages/com.zxxuh.dialogue-system/Samples~/Basic Dialogue/`
- Move: `Assets/DialogueSystem/Samples/DialogueSystemCanvas.prefab*` → `Packages/com.zxxuh.dialogue-system/Samples~/Basic Dialogue/`
- Move: `Assets/DialogueSystem/Samples/01_AncientCityTour` → `Packages/com.zxxuh.dialogue-system/Samples~/Guided Tours/01_AncientCityTour`
- Move: `Assets/DialogueSystem/Samples/02_AbandonedLabTour` → `Packages/com.zxxuh.dialogue-system/Samples~/Guided Tours/02_AbandonedLabTour`
- Move: `Assets/DialogueSystem/Samples/03_RainyStreetTour` → `Packages/com.zxxuh.dialogue-system/Samples~/Guided Tours/03_RainyStreetTour`
- Remove after verification: obsolete empty `Assets/DialogueSystem` container and its empty `Art` metadata
- Verify: `Packages/com.zxxuh.dialogue-system/package.json`

**Interfaces:**
- Consumes: the two `samples` entries already declared in `package.json` and all existing sample `.meta` GUIDs.
- Produces: Package Manager importable `Basic Dialogue` and `Guided Tours` sample groups.

- [ ] **Step 1: Capture sample GUIDs before moving**

Run `rg -n "^guid:" Assets/DialogueSystem/Samples -g "*.meta"` and save the output. Expected: every scene, asset, prefab, and nested folder has a GUID.

- [ ] **Step 2: Move the basic sample with metadata**

Create `Samples~/Basic Dialogue`, then move the three base resources and their `.meta` files using exact `Move-Item -LiteralPath` calls. Do not move `Assets/DialogueSystem/Samples.meta` to `Samples~.meta`; the tilde directory is an ignored UPM container.

```powershell
New-Item -ItemType Directory -Path "Packages/com.zxxuh.dialogue-system/Samples~/Basic Dialogue" -Force
foreach ($name in @("DialogueSystemSample.unity", "DialogueSystemSample.asset", "DialogueSystemCanvas.prefab")) {
    Move-Item -LiteralPath "Assets/DialogueSystem/Samples/$name" -Destination "Packages/com.zxxuh.dialogue-system/Samples~/Basic Dialogue/$name"
    Move-Item -LiteralPath "Assets/DialogueSystem/Samples/$name.meta" -Destination "Packages/com.zxxuh.dialogue-system/Samples~/Basic Dialogue/$name.meta"
}
```

- [ ] **Step 3: Move guided-tour samples with metadata**

Create `Samples~/Guided Tours`, then move each of the three tour folders and its sibling `.meta` file. Verify no non-meta sample content remains under `Assets/DialogueSystem/Samples`.

```powershell
New-Item -ItemType Directory -Path "Packages/com.zxxuh.dialogue-system/Samples~/Guided Tours" -Force
foreach ($name in @("01_AncientCityTour", "02_AbandonedLabTour", "03_RainyStreetTour")) {
    Move-Item -LiteralPath "Assets/DialogueSystem/Samples/$name" -Destination "Packages/com.zxxuh.dialogue-system/Samples~/Guided Tours/$name"
    Move-Item -LiteralPath "Assets/DialogueSystem/Samples/$name.meta" -Destination "Packages/com.zxxuh.dialogue-system/Samples~/Guided Tours/$name.meta"
}
```

- [ ] **Step 4: Remove only verified-empty legacy containers**

List `Assets/DialogueSystem` recursively. After confirming it contains only the now-empty `Samples`, empty `Art` containers, and their `.meta` files, remove those exact paths with `Remove-Item -LiteralPath`. The deletion is recoverable from Git and is limited to obsolete empty migration containers.

- [ ] **Step 5: Verify GUID preservation and package sample declarations**

Run:

```powershell
rg -n "^guid:" "Packages/com.zxxuh.dialogue-system/Samples~" -g "*.meta"
$package = Get-Content -LiteralPath "Packages/com.zxxuh.dialogue-system/package.json" -Raw | ConvertFrom-Json
$package.samples | Select-Object displayName,path
```

Expected: captured resource GUID lines match Step 1; sample paths are `Samples~/Basic Dialogue` and `Samples~/Guided Tours`.

- [ ] **Step 6: Validate shipped scene references statically**

Collect every `guid:` reference in `.unity`, `.prefab`, and `.asset` files under `Samples~`, then verify referenced script/font/asset GUIDs are present in package `.meta` files or are Unity built-ins. If any missing GUID is found, stop and fix the move instead of regenerating sample assets.

- [ ] **Step 7: Import Samples in Unity when available**

Expected manual/automated result: both sample groups expose an Import button; imported scenes appear below `Assets/Samples/Dialogue System/1.0.0/`, open without missing scripts, and keep the bundled font references. If Unity remains unavailable, record this step as not run.

- [ ] **Step 8: Commit the Samples migration**

```powershell
git commit -m "feat: publish dialogue demos as UPM samples"
```

---

### Task 4: Add package documentation and license notices

**Files:**
- Create: `Packages/com.zxxuh.dialogue-system/README.md`
- Create: `Packages/com.zxxuh.dialogue-system/CHANGELOG.md`
- Create: `Packages/com.zxxuh.dialogue-system/LICENSE.md`
- Create: `Packages/com.zxxuh.dialogue-system/Third Party Notices.md`
- Modify: `README.md`
- Modify: `docs/DialogueSystem-Tour-Samples.md`

**Interfaces:**
- Consumes: current `README.md`, root `LICENSE`, font `OFL.txt`, package ID/version, current Git remote.
- Produces: complete installation, Samples, generator, API, testing, license, and release documentation.

- [ ] **Step 1: Write a documentation path check that currently fails**

Run:

```powershell
rg -n "Assets/DialogueSystem/(Samples|Fonts|Runtime|Editor|Tests)" README.md docs/DialogueSystem-Tour-Samples.md
```

Expected: the old sample and font paths are reported.

- [ ] **Step 2: Create the package README**

Adapt the current bilingual README into `Packages/com.zxxuh.dialogue-system/README.md`. It must include these exact installation forms:

```text
https://github.com/OpenFrQuSh/Dialogue-System-.git?path=/Packages/com.zxxuh.dialogue-system#v1.0.0
file:../dist/com.zxxuh.dialogue-system-1.0.0.tgz
```

Document Unity `2022.3`, UGUI, TextMeshPro, Samples import, `Assets/DialogueSystemGenerated`, DialogueAsset creation, Runner/View binding, menus, tests, and Noto Sans SC licensing.

- [ ] **Step 3: Create changelog and license files**

Create `CHANGELOG.md` with a `1.0.0 - 2026-08-29` release describing the UPM package, runtime/editor split, Samples, tests, font, and package-safe generators. Copy the full root Apache 2.0 `LICENSE` content to package `LICENSE.md` without modifying its terms.

Use this changelog content:

```markdown
# Changelog

All notable changes to this package are documented in this file.

## [1.0.0] - 2026-08-29

### Added

- Branching dialogue runtime with variables, history, auto play, speed, and skip controls.
- UGUI and TextMeshPro presentation components.
- Guided camera-tour components and four importable Samples.
- Editor inspectors, validation, package-safe sample generators, and EditMode/PlayMode tests.
- Bundled Noto Sans SC font assets and license notices.
```

Copy the license mechanically:

```powershell
Copy-Item -LiteralPath "LICENSE" -Destination "Packages/com.zxxuh.dialogue-system/LICENSE.md"
```

- [ ] **Step 4: Create third-party notices**

`Third Party Notices.md` must identify Noto Sans SC, state that it is licensed under SIL Open Font License 1.1, point to `Fonts/OFL.txt`, and explain that the source `.ttf` and derived TMP font asset are distributed with the package.

Use this exact notice:

```markdown
# Third Party Notices

## Noto Sans SC

This package distributes `Fonts/NotoSansSC-Variable.ttf` and the derived TextMeshPro font asset `Fonts/NotoSansSC-Dynamic.asset`.

Noto Sans SC is licensed under the SIL Open Font License, Version 1.1. The complete license text is included at `Fonts/OFL.txt`.
```

- [ ] **Step 5: Update root user documentation**

Make the root README the development-repository entry point while retaining the feature and API overview. Replace shipped sample paths with Package Manager import instructions and replace generator output paths with `Assets/DialogueSystemGenerated`. Update `docs/DialogueSystem-Tour-Samples.md` the same way.

Do not rewrite files below `docs/superpowers/specs` or `docs/superpowers/plans`; they are historical records of earlier development stages.

- [ ] **Step 6: Re-run documentation checks**

Run the Step 1 `rg` command again. Expected: no matches. Also run `rg -n "com.zxxuh.dialogue-system|Assets/DialogueSystemGenerated" README.md Packages/com.zxxuh.dialogue-system/README.md docs/DialogueSystem-Tour-Samples.md`; expected: installation and generation guidance appears in all relevant documents.

- [ ] **Step 7: Commit documentation**

```powershell
git commit -m "docs: document UPM installation and samples"
```

---

### Task 5: Add a reproducible UPM packer and build the tarball

**Files:**
- Create: `scripts/pack-upm.ps1`
- Modify: `.gitignore`
- Generate: `dist/com.zxxuh.dialogue-system-1.0.0.tgz`

**Interfaces:**
- Consumes: package root `Packages/com.zxxuh.dialogue-system`, `package.json`, npm, and tar.
- Produces: validated npm-format tarball with `package/package.json`, Runtime, Editor, Fonts, Samples, Tests, and documentation.

- [ ] **Step 1: Run a failing precondition check**

Before the script exists, run `Test-Path -LiteralPath scripts/pack-upm.ps1`. Expected: `False`.

- [ ] **Step 2: Implement `scripts/pack-upm.ps1`**

The script must:

1. Enable strict mode and stop on errors.
2. Resolve the repository root from `$PSScriptRoot` without using `$HOME` or `~`.
3. Parse `package.json` and assert name `com.zxxuh.dialogue-system`, version `1.0.0`, Unity `2022.3`, both dependencies, both samples, and all required files/directories.
4. Search package C# and current Markdown files for stale `Assets/DialogueSystem/` paths and fail with the matching paths.
5. Create the exact repository-local `dist` directory.
6. Remove only the exact prior archive path after confirming its parent is the resolved `dist` directory.
7. Invoke `npm pack Packages/com.zxxuh.dialogue-system --pack-destination dist` using resolved absolute paths.
8. Assert npm emitted exactly `com.zxxuh.dialogue-system-1.0.0.tgz`.
9. List the archive with `tar -tf` and require `package/package.json`, `package/Runtime/DialogueSystem.Runtime.asmdef`, `package/Editor/DialogueSystem.Editor.asmdef`, `package/Fonts/OFL.txt`, both `package/Samples~` groups, both test assemblies, `package/LICENSE.md`, and `package/Third Party Notices.md`.
10. Reject archive entries containing `Library/`, `Logs/`, `obj/`, `ProjectSettings/`, `MCP`, or `Assets/DialogueSystemGenerated`.
11. Print the resolved artifact path and byte size.

Use comments immediately above path validation, archive replacement, and tar-content validation to explain the safety intent.

Use this complete script:

```powershell
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$packageRoot = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot "Packages/com.zxxuh.dialogue-system")).Path
$manifestPath = Join-Path $packageRoot "package.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$distRoot = Join-Path $repositoryRoot "dist"
$archiveName = "com.zxxuh.dialogue-system-1.0.0.tgz"
$archivePath = Join-Path $distRoot $archiveName

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)] $Actual,
        [Parameter(Mandatory = $true)] $Expected,
        [Parameter(Mandatory = $true)] [string] $Label
    )

    if ($Actual -ne $Expected) {
        throw "$Label must be '$Expected' but was '$Actual'."
    }
}

Assert-Equal $manifest.name "com.zxxuh.dialogue-system" "Package name"
Assert-Equal $manifest.version "1.0.0" "Package version"
Assert-Equal $manifest.unity "2022.3" "Minimum Unity version"
Assert-Equal $manifest.dependencies."com.unity.ugui" "1.0.0" "UGUI dependency"
Assert-Equal $manifest.dependencies."com.unity.textmeshpro" "3.0.7" "TextMeshPro dependency"

$samplePaths = @($manifest.samples | ForEach-Object { $_.path })
foreach ($requiredSample in @("Samples~/Basic Dialogue", "Samples~/Guided Tours")) {
    if ($requiredSample -notin $samplePaths) {
        throw "Missing package sample declaration: $requiredSample"
    }
}

$requiredPaths = @(
    "Runtime/DialogueSystem.Runtime.asmdef",
    "Editor/DialogueSystem.Editor.asmdef",
    "Fonts/NotoSansSC-Dynamic.asset",
    "Fonts/NotoSansSC-Variable.ttf",
    "Fonts/OFL.txt",
    "Samples~/Basic Dialogue",
    "Samples~/Guided Tours",
    "Tests/Editor/DialogueSystem.EditModeTests.asmdef",
    "Tests/Runtime/DialogueSystem.PlayModeTests.asmdef",
    "README.md",
    "CHANGELOG.md",
    "LICENSE.md",
    "Third Party Notices.md"
)

foreach ($relativePath in $requiredPaths) {
    if (-not (Test-Path -LiteralPath (Join-Path $packageRoot $relativePath))) {
        throw "Required package path is missing: $relativePath"
    }
}

$stalePaths = Get-ChildItem -LiteralPath $packageRoot -Recurse -File |
    Where-Object { $_.Extension -in @(".cs", ".md") } |
    Select-String -SimpleMatch "Assets/DialogueSystem/"
if ($stalePaths) {
    throw "Stale pre-UPM paths remain:`n$($stalePaths -join [Environment]::NewLine)"
}

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
$resolvedDistRoot = (Resolve-Path -LiteralPath $distRoot).Path

# 只允许替换仓库 dist 目录中的精确版本归档，避免路径计算错误时删除其他文件。
if ([IO.Path]::GetDirectoryName($archivePath) -ne $resolvedDistRoot) {
    throw "Archive escaped the dist directory: $archivePath"
}
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$npmCommand = (Get-Command npm.cmd -ErrorAction Stop).Source
& $npmCommand pack $packageRoot --pack-destination $resolvedDistRoot
if ($LASTEXITCODE -ne 0) {
    throw "npm pack failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path -LiteralPath $archivePath)) {
    throw "npm pack did not create the expected archive: $archivePath"
}

$archiveEntries = @(& tar -tf $archivePath)
if ($LASTEXITCODE -ne 0) {
    throw "tar could not list the generated archive."
}

# 独立检查归档内容，确保发布物包含 UPM 必需文件且没有泄漏宿主工程产物。
$requiredEntries = @(
    "package/package.json",
    "package/Runtime/DialogueSystem.Runtime.asmdef",
    "package/Editor/DialogueSystem.Editor.asmdef",
    "package/Fonts/OFL.txt",
    "package/Samples~/Basic Dialogue/DialogueSystemSample.unity",
    "package/Samples~/Guided Tours/01_AncientCityTour/AncientCityTour.unity",
    "package/Tests/Editor/DialogueSystem.EditModeTests.asmdef",
    "package/Tests/Runtime/DialogueSystem.PlayModeTests.asmdef",
    "package/LICENSE.md",
    "package/Third Party Notices.md"
)
foreach ($entry in $requiredEntries) {
    if ($entry -notin $archiveEntries) {
        throw "Archive is missing required entry: $entry"
    }
}

$forbiddenEntryPatterns = @(
    "(^|/)Library/",
    "(^|/)Logs/",
    "(^|/)obj/",
    "(^|/)ProjectSettings/",
    "MCP",
    "Assets/DialogueSystemGenerated"
)
foreach ($pattern in $forbiddenEntryPatterns) {
    $match = $archiveEntries | Where-Object { $_ -match $pattern } | Select-Object -First 1
    if ($match) {
        throw "Archive contains forbidden entry '$match' for pattern '$pattern'."
    }
}

$artifact = Get-Item -LiteralPath $archivePath
Write-Output "UPM package created: $($artifact.FullName)"
Write-Output "Size: $($artifact.Length) bytes"
```

- [ ] **Step 3: Ignore generated release artifacts**

Append this section to `.gitignore`:

```gitignore
# Reproducible UPM release artifacts
dist/
```

- [ ] **Step 4: Run the packer**

Run:

```powershell
pwsh -NoProfile -File scripts/pack-upm.ps1
```

Expected: exit code `0` and final artifact `D:\demo\Dialogue System Plugin\dist\com.zxxuh.dialogue-system-1.0.0.tgz`.

- [ ] **Step 5: Inspect the archive independently**

Run:

```powershell
tar -tf "dist/com.zxxuh.dialogue-system-1.0.0.tgz"
Get-Item -LiteralPath "dist/com.zxxuh.dialogue-system-1.0.0.tgz" | Select-Object FullName,Length,LastWriteTime
```

Expected: required paths are present and no host-project paths appear.

- [ ] **Step 6: Commit the reproducible packer**

Do not stage `dist`. Commit only the script and `.gitignore`:

```powershell
git commit -m "build: add reproducible UPM package artifact"
```

---

### Task 6: Perform final verification and report environment-limited checks

**Files:**
- Verify: `Packages/com.zxxuh.dialogue-system/**`
- Verify: `Packages/manifest.json`
- Verify: `dist/com.zxxuh.dialogue-system-1.0.0.tgz`
- Verify unchanged: `ProjectSettings/EditorSettings.asset`

**Interfaces:**
- Consumes: completed package, host project, tests, Samples, and tarball.
- Produces: evidence-backed release status with explicit passed and unrun checks.

- [ ] **Step 1: Run all static package checks**

Run:

```powershell
pwsh -NoProfile -File scripts/pack-upm.ps1
rg -n "Assets/DialogueSystem/(Samples|Fonts|Runtime|Editor|Tests)" Packages/com.zxxuh.dialogue-system README.md docs/DialogueSystem-Tour-Samples.md
git diff --check
git status --short
```

Expected: packer passes; stale-path search has no matches; `git diff --check` is clean; only the user-owned `ProjectSettings/EditorSettings.asset` remains modified plus any intentional uncommitted implementation files.

- [ ] **Step 2: Verify package manifest consistency**

Parse both manifests with `ConvertFrom-Json`. Confirm package name/version/dependencies/samples and project `testables`. Confirm no `com.coplaydev.unity-mcp` dependency exists inside the plugin package.

- [ ] **Step 3: Run EditMode and PlayMode tests if Unity becomes available**

Run the complete `DialogueSystem.EditModeTests` and `DialogueSystem.PlayModeTests` assemblies. Expected: all pass and Console has zero compile errors. If the executable remains unavailable, report both assemblies as `NOT RUN — Unity Editor executable unavailable`.

- [ ] **Step 4: Run clean-install and Samples smoke tests if Unity becomes available**

Create a temporary Unity `2022.3` project, install the local `.tgz`, import both Samples groups, open all four scenes, run each once, and invoke both generator menus twice. Expected: no missing scripts/fonts, no compiler errors, and writes occur only below `Assets/DialogueSystemGenerated`. If Unity remains unavailable, report this complete group as not run.

- [ ] **Step 5: Recheck the preserved user setting**

Run `git hash-object -- ProjectSettings/EditorSettings.asset` and compare it with Task 1 Step 1. Expected: identical hash and still not staged.

- [ ] **Step 6: Final scope review**

Run `git log --oneline -6` and `git status --short`. Review each implementation commit against the spec. Confirm no unrelated files were modified or committed and the tarball exists at the exact release path.
