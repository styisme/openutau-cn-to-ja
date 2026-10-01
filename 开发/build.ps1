<#
.SYNOPSIS
    ChineseToJapanesePhonemizer v2.0 Auto Build Script

.DESCRIPTION
    1. Verify required files (.cs + 3 DLLs)
    2. Generate csproj if missing
    3. Run dotnet build
    4. Output compiled DLL path
    5. Optionally deploy to OpenUtau Plugins folder

.PARAMETER OpenUtauDir
    Optional. Absolute path to OpenUtau install directory.
    Example: -OpenUtauDir "G:\OpenUtau-win-x64 (1)"

.PARAMETER Framework
    Optional. Target framework. Default: net10.0.
    Use net8.0 for older OpenUtau.

.PARAMETER AutoDeploy
    Optional. Deploy without confirmation.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Framework net8.0
    .\build.ps1 -OpenUtauDir "G:\OpenUtau-win-x64 (1)" -AutoDeploy
#>

param(
    [string]$OpenUtauDir = "",
    [string]$Framework = "net10.0",
    [switch]$AutoDeploy
)

$ErrorActionPreference = "Stop"

function Write-Info { param($msg) Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Ok   { param($msg) Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Err  { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host " ChineseToJapanesePhonemizer v2.0 Build" -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host ""

# ---- Step 0: check dotnet ----
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Ok "dotnet SDK: $dotnetVersion"
} catch {
    Write-Err "dotnet not found. Install .NET SDK: https://dotnet.microsoft.com/download"
    exit 1
}

# ---- Step 1: verify required files ----
Write-Info "Checking required files..."

$requiredCs = "ChineseToJapanesePhonemizer.cs"
$requiredDlls = @(
    "OpenUtau.Core.dll",
    "OpenUtau.Plugin.Builtin.dll",
    "WanaKanaNet.dll"
)

$missing = @()
if (-not (Test-Path $requiredCs)) { $missing += $requiredCs }
foreach ($dll in $requiredDlls) {
    if (-not (Test-Path $dll)) { $missing += $dll }
}

if ($missing.Count -gt 0) {
    Write-Err "Missing files:"
    foreach ($f in $missing) { Write-Host "    - $f" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Copy these from OpenUtau install directory:" -ForegroundColor Yellow
    Write-Host "    OpenUtau.Core.dll" -ForegroundColor Yellow
    Write-Host "    OpenUtau.Plugin.Builtin.dll" -ForegroundColor Yellow
    Write-Host "    WanaKanaNet.dll" -ForegroundColor Yellow
    exit 1
}
Write-Ok "All required files present"

# ---- Step 2: generate csproj if missing ----
$csproj = "MyZHtoJAPlugin.csproj"
if (-not (Test-Path $csproj)) {
    Write-Info "Generating $csproj ..."

    $lines = @(
        '<Project Sdk="Microsoft.NET.Sdk">',
        '  <PropertyGroup>',
        "    <TargetFramework>$Framework</TargetFramework>",
        '    <AssemblyName>MyZHtoJAPlugin</AssemblyName>',
        '    <RootNamespace>OpenUtau.Plugin.Builtin</RootNamespace>',
        '    <LangVersion>latest</LangVersion>',
        '    <Nullable>disable</Nullable>',
        '    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>',
        '    <NoWarn>CS8632</NoWarn>',
        '  </PropertyGroup>',
        '  <ItemGroup>',
        '    <Compile Include="ChineseToJapanesePhonemizer.cs" />',
        '  </ItemGroup>',
        '  <ItemGroup>',
        '    <Reference Include="OpenUtau.Core">',
        '      <HintPath>OpenUtau.Core.dll</HintPath>',
        '      <Private>false</Private>',
        '    </Reference>',
        '    <Reference Include="OpenUtau.Plugin.Builtin">',
        '      <HintPath>OpenUtau.Plugin.Builtin.dll</HintPath>',
        '      <Private>false</Private>',
        '    </Reference>',
        '    <Reference Include="WanaKanaNet">',
        '      <HintPath>WanaKanaNet.dll</HintPath>',
        '      <Private>false</Private>',
        '    </Reference>',
        '  </ItemGroup>',
        '</Project>'
    )
    $content = $lines -join [Environment]::NewLine
    Set-Content -Path $csproj -Value $content -Encoding UTF8
    Write-Ok "Generated $csproj (TargetFramework: $Framework)"
} else {
    Write-Info "$csproj already exists, skipping generation"
}

# ---- Step 3: clean old build ----
if (Test-Path "bin") {
    Write-Info "Cleaning bin/ ..."
    Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Write-Info "Cleaning obj/ ..."
    Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue
}

# ---- Step 4: build ----
Write-Host ""
Write-Info "Building (dotnet build -c Release) ..."
Write-Host ""

& dotnet build -c Release
$buildExitCode = $LASTEXITCODE

Write-Host ""

if ($buildExitCode -ne 0) {
    Write-Err "Build failed (exit code: $buildExitCode)"
    Write-Host ""
    Write-Host "Common errors:" -ForegroundColor Yellow
    Write-Host "  CS1705  Version conflict -> wrong DLL version, re-download from official" -ForegroundColor Yellow
    Write-Host "  CS0246  Type not found  -> missing reference DLL" -ForegroundColor Yellow
    Write-Host "  CS1012  Too many chars  -> composite chars in code" -ForegroundColor Yellow
    exit $buildExitCode
}

Write-Ok "Build succeeded"

# ---- Step 5: locate output ----
$outputDir = "bin\Release\$Framework"
$outputDll = Join-Path $outputDir "MyZHtoJAPlugin.dll"

if (-not (Test-Path $outputDll)) {
    Write-Err "Output not found: $outputDll"
    exit 1
}

$dllInfo = Get-Item $outputDll
$sizeKB = [math]::Round($dllInfo.Length / 1KB, 2)

Write-Host ""
Write-Host "---------------------------------------------" -ForegroundColor Green
Write-Host " Build Output:" -ForegroundColor Green
Write-Host "   Path: $($dllInfo.FullName)" -ForegroundColor Green
Write-Host "   Size: $sizeKB KB" -ForegroundColor Green
Write-Host "   Time: $($dllInfo.LastWriteTime)" -ForegroundColor Green
Write-Host "---------------------------------------------" -ForegroundColor Green

# Warn if other DLLs are in output
$otherDlls = Get-ChildItem -Path $outputDir -Filter "*.dll" | Where-Object { $_.Name -ne "MyZHtoJAPlugin.dll" }
if ($otherDlls.Count -gt 0) {
    Write-Warn "Extra DLLs found in output (do NOT copy these to Plugins):"
    foreach ($f in $otherDlls) { Write-Host "    - $($f.Name)" -ForegroundColor Yellow }
}

# ---- Step 6: optional deploy ----
if ($OpenUtauDir -ne "" -and (Test-Path $OpenUtauDir)) {
    Write-Host ""
    Write-Info "OpenUtau dir: $OpenUtauDir"

    $pluginsDir = Join-Path $OpenUtauDir "Plugins"
    if (-not (Test-Path $pluginsDir)) {
        Write-Info "Creating Plugins dir: $pluginsDir"
        New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
    }

    $targetDll = Join-Path $pluginsDir "MyZHtoJAPlugin.dll"

    $shouldDeploy = $AutoDeploy
    if (-not $shouldDeploy) {
        $answer = Read-Host "Copy MyZHtoJAPlugin.dll to Plugins folder? (y/N)"
        $shouldDeploy = ($answer -eq "y" -or $answer -eq "Y")
    }

    if ($shouldDeploy) {
        $running = Get-Process -Name "OpenUtau" -ErrorAction SilentlyContinue
        if ($running) {
            Write-Warn "OpenUtau is running! Close it first or DLL will be locked."
            $answer = Read-Host "Try to copy anyway? (y/N)"
            if ($answer -ne "y" -and $answer -ne "Y") {
                Write-Info "Deployment cancelled."
                exit 0
            }
        }

        Copy-Item -Path $outputDll -Destination $targetDll -Force

        if (Test-Path $targetDll) {
            $t = Get-Item $targetDll
            $tSizeKB = [math]::Round($t.Length / 1KB, 2)
            Write-Ok "Deployed: $($t.FullName)"
            Write-Ok "Size: $tSizeKB KB"
            Write-Host ""
            Write-Host "Now start OpenUtau and select 'ZH to JA' phonemizer." -ForegroundColor Green
        } else {
            Write-Err "Deployment failed."
        }
    } else {
        Write-Info "Skipped deployment"
    }
} elseif ($OpenUtauDir -ne "") {
    Write-Warn "OpenUtau directory not found: $OpenUtauDir"
}

Write-Host ""
Write-Ok "Done."
Write-Host ""
Write-Host "Manual deploy: copy the following file to OpenUtau\Plugins\ :" -ForegroundColor Cyan
Write-Host "  $outputDll" -ForegroundColor Cyan
Write-Host ""