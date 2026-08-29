# Dialogue System

`com.zxxuh.dialogue-system` is a reusable Unity UGUI and TextMeshPro dialogue package. It includes branching dialogue data, variables, history, auto play, speed and skip controls, guided camera tours, editor validation, and importable samples.

The [source repository](https://github.com/OpenFrQuSh/Dialogue-System-) also contains the complete Unity development and demo project. Package Manager installs only this package directory when you use the Git URL below.

## Requirements

- Unity `2022.3` or newer within the 2022.3 LTS line
- UGUI `1.0.0`
- TextMeshPro `3.0.7`

The package manifest declares UGUI and TextMeshPro as direct dependencies.

## Installation

In Unity Package Manager, choose **Add package from git URL** and enter:

```text
https://github.com/OpenFrQuSh/Dialogue-System-.git?path=/Packages/com.zxxuh.dialogue-system#v1.0.0
```

Repository maintainers can run `scripts/pack-upm.ps1` from the source repository to create a local release archive. The generated `dist` directory is intentionally not committed. After keeping or copying the archive to a location accessible to a consuming project, add it to that project's `Packages/manifest.json` using the appropriate relative path, for example:

```text
file:../dist/com.zxxuh.dialogue-system-1.0.0.tgz
```

## Samples

Open Package Manager, select **Dialogue System**, expand **Samples**, then import:

- **Basic Dialogue** — branching dialogue, choices, and history.
- **Guided Tours** — three Chinese tours demonstrating branching, auto play, skip, and camera movement.

Unity imports package samples below `Assets/Samples/Dialogue System/1.0.0/`. The bundled Noto Sans SC font remains referenced from the installed package.

## Quick Start

Create a dialogue asset from:

```text
Assets > Create > Dialogue System > Dialogue Asset
```

Configure the entry node, variables, and `Line`, `Choice`, or `End` nodes. In your scene, bind a `DialogueView` to a `DialogueRunner`, then start the asset:

```csharp
dialogueView.Bind(dialogueRunner);
dialogueRunner.StartDialogue(dialogueAsset);
```

The imported samples contain complete Runner/View bindings that can be copied into a game scene.

## Editor Generators

These menu commands rebuild editable examples under `Assets/DialogueSystemGenerated/Samples`:

```text
Tools > Dialogue System > Create Sample Scene
Tools > Dialogue System > Create Guided Tour Samples
```

The generators only create or replace their fixed resources inside `Assets/DialogueSystemGenerated`; they do not modify installed package files or unrelated user assets.

## Tests

The package contains EditMode and PlayMode tests. Add `com.zxxuh.dialogue-system` to the host project's `testables` array, then run both suites from:

```text
Window > General > Test Runner
```

## License

The package code is licensed under Apache License 2.0; see `LICENSE.md`.

The bundled `Fonts/NotoSansSC-Variable.ttf` and derived TextMeshPro font asset use the SIL Open Font License 1.1. See `Fonts/OFL.txt` and `Third Party Notices.md`.
