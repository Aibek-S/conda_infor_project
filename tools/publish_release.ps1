param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$PythonCommand = "python",
    [switch]$SkipCollectorBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "conda_infor_project\conda_infor_project.csproj"
$publishDir = Join-Path $repoRoot "publish\$Runtime"
$env:DOTNET_CLI_HOME = $repoRoot
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"

if (-not $SkipCollectorBuild) {
    & (Join-Path $PSScriptRoot "build_collector.ps1") -PythonCommand $PythonCommand
}

Write-Host "[publish] Publishing WinForms app..."
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host "[publish] Done: $publishDir"
Write-Host "[publish] Send this folder to another Windows PC and run conda_infor_project.exe."
