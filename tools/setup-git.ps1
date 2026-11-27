<#
.SYNOPSIS
    One-command Git, Git LFS, Hooks, and Unity SmartMerge bootstrap for SE220_ThietKeGame_.
.EXAMPLE
    pwsh -ExecutionPolicy Bypass -File .\tools\setup-git.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

Write-Host "==> Configuring local Git repository settings in $RepoRoot..." -ForegroundColor Cyan
git -C $RepoRoot config --local core.hooksPath .githooks
git -C $RepoRoot config --local core.longpaths true
git -C $RepoRoot config --local core.autocrlf true
git -C $RepoRoot config --local rerere.enabled true
git -C $RepoRoot config --local fetch.prune true

# Verify Git LFS availability
if (Get-Command git-lfs -ErrorAction SilentlyContinue) {
    $lfsVer = (& git -C $RepoRoot lfs version)
    Write-Host "==> Git LFS detected: $lfsVer" -ForegroundColor Green
} else {
    Write-Warning "Git LFS is not installed! Install it via 'winget install GitHub.GitLFS' before pushing binary assets."
}

# Read required Unity version from ProjectVersion.txt
$versionFile = Join-Path $RepoRoot 'ProjectSettings\ProjectVersion.txt'
$unityVersion = '6000.6.9f1'
if (Test-Path $versionFile) {
    $match = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(\S+)'
    if ($match) {
        $unityVersion = $match.Matches[0].Groups[1].Value
    }
}

# Locate UnityYAMLMerge.exe across common Windows Unity Hub paths
$candidates = @(
    "D:\Unity\$unityVersion\Editor\Data\Tools\UnityYAMLMerge.exe",
    "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Data\Tools\UnityYAMLMerge.exe",
    "D:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Data\Tools\UnityYAMLMerge.exe",
    "$env:ProgramFiles\Unity\Hub\Editor\$unityVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
)

$yamlMerge = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $yamlMerge) {
    $unityCli = Join-Path $env:LOCALAPPDATA 'Unity\bin\unity.exe'
    if (Test-Path $unityCli) {
        try {
            & $unityCli vcs merge-setup --project-path $RepoRoot --quiet | Out-Null
            Write-Host "==> Configured UnityYAMLMerge via Unity CLI." -ForegroundColor Green
        } catch {
            Write-Warning "UnityYAMLMerge not found for Unity $unityVersion. Install Unity $unityVersion and re-run setup-git.ps1."
        }
    } else {
        Write-Warning "UnityYAMLMerge.exe not found in default paths for Unity $unityVersion. Set merge.unityyamlmerge.driver manually if installed elsewhere."
    }
} else {
    $normalized = $yamlMerge -replace '\\', '/'
    git -C $RepoRoot config --local merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
    git -C $RepoRoot config --local merge.unityyamlmerge.driver "'$normalized' merge -h -p --force --fallback none %O %B %A %A"
    git -C $RepoRoot config --local merge.unityyamlmerge.recursive binary
    Write-Host "==> Configured UnityYAMLMerge: $normalized" -ForegroundColor Green
}

Write-Host "==> Git setup complete! Active hooks path: $(git -C $RepoRoot config --local --get core.hooksPath)" -ForegroundColor Cyan
