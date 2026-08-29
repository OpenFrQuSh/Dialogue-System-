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

# 发布前逐项约束路径与文件，避免从错误目录或残缺包生成看似成功的归档。
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
