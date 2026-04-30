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

    $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -Path $vswherePath -PathType Leaf) {
        $requiredComponents = @(
            'Microsoft.Component.MSBuild',
            'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'
        )
        $detectedPath = & $vswherePath -latest -products * -requires $requiredComponents -find 'MSBuild\**\Bin\MSBuild.exe' |
            Select-Object -First 1
        if ($detectedPath) {
            return $detectedPath
        }
    }

    $msbuildCommand = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuildCommand) {
        return $msbuildCommand.Source
    }

    throw "Unable to locate MSBuild.exe with the C++ x86/x64 toolchain. Install Visual Studio Build Tools with Desktop development with C++ or pass -MSBuildPath."
}

function Ensure-TrailingSlash([string]$PathValue) {
    $separator = [System.IO.Path]::DirectorySeparatorChar
    if ($PathValue.EndsWith($separator)) {
        return $PathValue
    }
    return $PathValue + $separator
}

function Write-LaunchInstructions(
    [string]$PlatformName,
    [string]$PlatformOutDir
) {
    $instructionsPath = Join-Path $PlatformOutDir 'README-launch.txt'

    $platformSpecificDotnet = if ($PlatformName -eq 'x86') {
        '${env:ProgramFiles(x86)}\dotnet\dotnet.exe'
    } else {
        'dotnet'
    }

    $pathDotnetNote = if ($PlatformName -eq 'x86') {
        @(
            ''
            'Do not use `dotnet .\ManagedSpy.dll` from the x86 folder unless `dotnet` resolves'
            'to the x86 installation. On most machines `dotnet` on PATH is x64 and will fail'
            'with an architecture mismatch for the x86 build.'
        )
    } else {
        @()
    }

    $content = @(
        "ManagedSpy $PlatformName launch instructions"
        ''
        'Preferred launch:'
        '  .\ManagedSpy.exe'
        ''
        'If you need to launch the DLL directly, use a host with the same architecture:'
        "  & `"$platformSpecificDotnet`" .\ManagedSpy.dll"
    ) + $pathDotnetNote + @(
        ''
        'These builds are framework-dependent and require the matching .NET Desktop runtime.'
        ''
    )

    Set-Content -Path $instructionsPath -Value $content -Encoding Ascii
    Write-BuildLog "Wrote launch instructions to $instructionsPath"
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

    Write-LaunchInstructions -PlatformName $PlatformName -PlatformOutDir $platformOutDir
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
