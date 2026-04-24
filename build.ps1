param(
    [string]$Solution = "$PSScriptRoot\ManagedSpy.sln",
    [ValidateSet('x86', 'x64', 'both')]
    [string]$Platform = 'both',
    [string]$Configuration = 'Release',
    [string]$OutputRoot = "$PSScriptRoot\artifacts\release",
    [string]$MSBuildPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-BuildLog([string]$Message, [string]$Header = 'build') {
    Write-Host "[$Header] $Message" -ForegroundColor White
}

function Resolve-MSBuildPath([string]$PreferredPath) {
    if ($PreferredPath) {
        if (!(Test-Path -Path $PreferredPath -PathType Leaf)) {
            throw "MSBuild path does not exist: $PreferredPath"
        }
        return (Resolve-Path $PreferredPath).Path
    }

    $msbuildCommand = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuildCommand) {
        return $msbuildCommand.Source
    }

    $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -Path $vswherePath -PathType Leaf) {
        $detectedPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
            Select-Object -First 1
        if ($detectedPath) {
            return $detectedPath
        }
    }

    throw "Unable to locate MSBuild.exe. Install Visual Studio Build Tools or pass -MSBuildPath."
}

function Ensure-TrailingSlash([string]$PathValue) {
    $separator = [System.IO.Path]::DirectorySeparatorChar
    if ($PathValue.EndsWith($separator)) {
        return $PathValue
    }
    return $PathValue + $separator
}

function Invoke-ReleaseBuild(
    [string]$MSBuildExe,
    [string]$SolutionPath,
    [string]$ConfigurationName,
    [string]$PlatformName,
    [string]$OutDirRoot
) {
    $platformOutDir = [System.IO.Path]::GetFullPath((Join-Path $OutDirRoot $PlatformName))
    if (Test-Path -Path $platformOutDir) {
        Remove-Item -Path $platformOutDir -Recurse -Force
    }
    New-Item -Path $platformOutDir -ItemType Directory | Out-Null

    $msbuildOutDir = Ensure-TrailingSlash $platformOutDir
    Write-BuildLog "Building $ConfigurationName|$PlatformName => $msbuildOutDir"

    & $MSBuildExe $SolutionPath /t:Restore,Build /p:Configuration=$ConfigurationName /p:Platform=$PlatformName "/p:OutDir=$msbuildOutDir" /nologo /m /v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $ConfigurationName|$PlatformName (exit code $LASTEXITCODE)."
    }
}

$solutionPath = (Resolve-Path $Solution).Path
$resolvedMSBuild = Resolve-MSBuildPath $MSBuildPath
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

Write-BuildLog "Solution: $solutionPath"
Write-BuildLog "MSBuild: $resolvedMSBuild"
Write-BuildLog "Output root: $resolvedOutputRoot"

$platforms = switch ($Platform) {
    'both' { @('x86', 'x64') }
    default { @($Platform) }
}

foreach ($selectedPlatform in $platforms) {
    Invoke-ReleaseBuild -MSBuildExe $resolvedMSBuild -SolutionPath $solutionPath -ConfigurationName $Configuration -PlatformName $selectedPlatform -OutDirRoot $resolvedOutputRoot
}

Write-BuildLog "Release build complete." 'done'
Write-BuildLog "Artifacts are in $resolvedOutputRoot" 'done'
