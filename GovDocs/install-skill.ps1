#Requires -Version 5.1
<#
.SYNOPSIS
    Install the gov-docs Claude Code skill (and the dotnet template) in one command.

.DESCRIPTION
    1. Checks whether the `clean-arch` dotnet new template is already installed.
       If not, clones the repo to a temp directory and installs the template from there.
    2. Copies the skill file to $HOME\.claude\skills\gov-docs\SKILL.md.
    3. Prints next steps.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoUrl    = 'https://github.com/Son Pham/gov-docs.git'
$SkillSrc   = Join-Path $PSScriptRoot '.claude\skills\gov-docs\SKILL.md'
$SkillDest  = Join-Path $HOME '.claude\skills\gov-docs\SKILL.md'

function Write-Step([string]$Message) {
    Write-Host "  --> $Message" -ForegroundColor Cyan
}

function Write-Success([string]$Message) {
    Write-Host "  OK  $Message" -ForegroundColor Green
}

Write-Host ''
Write-Host 'gov-docs skill installer' -ForegroundColor White
Write-Host '----------------------------------' -ForegroundColor DarkGray

# ── 1. Ensure the dotnet new template is installed ─────────────────────────

Write-Step 'Checking dotnet new template clean-arch ...'

$listOutput = dotnet new list clean-arch 2>&1
$templateMissing = $listOutput -match 'No templates found'

if ($templateMissing) {
    Write-Step 'Template not found — installing from GitHub ...'

    $TempDir = Join-Path ([System.IO.Path]::GetTempPath()) "gov-docs-$([System.IO.Path]::GetRandomFileName())"

    try {
        git clone --depth 1 $RepoUrl $TempDir
        dotnet new install $TempDir
        Write-Success 'Template installed.'
    }
    finally {
        if (Test-Path $TempDir) {
            Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
} else {
    Write-Success 'Template already installed — skipping.'
}

# ── 2. Copy the skill file ──────────────────────────────────────────────────

Write-Step "Copying skill to $SkillDest ..."

$DestDir = Split-Path $SkillDest
if (-not (Test-Path $DestDir)) {
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

if (-not (Test-Path $SkillSrc)) {
    Write-Host "  ERROR: Skill source not found at $SkillSrc" -ForegroundColor Red
    Write-Host '         Run this script from the repo root, or clone the repo first.' -ForegroundColor Red
    exit 1
}

Copy-Item $SkillSrc $SkillDest -Force
Write-Success 'Skill installed.'

# ── 3. Done ────────────────────────────────────────────────────────────────

Write-Host ''
Write-Host 'Installation complete.' -ForegroundColor Green
Write-Host ''
Write-Host 'Next steps:'
Write-Host '  1. Restart Claude Code (so it picks up the new skill).'
Write-Host '  2. In any Claude Code session, type:'
Write-Host '       /gov-docs' -ForegroundColor Yellow
Write-Host '     or just describe what you want, e.g. "create a .NET backend for a blog".'
Write-Host ''
