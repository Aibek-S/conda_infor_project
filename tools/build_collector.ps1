param(
    [string]$PythonCommand = "python",
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot "conda_infor_project"
$collectorScript = Join-Path $projectDir "scripts\process_snapshot.py"
$distDir = Join-Path $repoRoot "collector_dist"
$buildDir = Join-Path $repoRoot "collector_build"
$specPath = Join-Path $repoRoot "process_snapshot.spec"
$outputExe = Join-Path $distDir "process_snapshot.exe"
$targetExe = Join-Path $projectDir "scripts\process_snapshot.exe"

if (-not (Test-Path $collectorScript)) {
    throw "Collector script not found: $collectorScript"
}

Write-Host "[collector] Checking Python..."
& $PythonCommand --version

if (-not $SkipInstall) {
    Write-Host "[collector] Installing build dependencies..."
    & $PythonCommand -m pip install --upgrade pip
    & $PythonCommand -m pip install --upgrade psutil pyinstaller
}

Write-Host "[collector] Building process_snapshot.exe..."
& $PythonCommand -m PyInstaller `
    --onefile `
    --clean `
    --name process_snapshot `
    --distpath $distDir `
    --workpath $buildDir `
    --specpath $repoRoot `
    $collectorScript

if (-not (Test-Path $outputExe)) {
    throw "Collector exe was not produced: $outputExe"
}

Copy-Item -LiteralPath $outputExe -Destination $targetExe -Force
Write-Host "[collector] Copied to $targetExe"

if (Test-Path $specPath) {
    Remove-Item -LiteralPath $specPath -Force
}

Write-Host "[collector] Done."
