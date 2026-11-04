[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $GameDirectory = "${env:ProgramFiles(x86)}\Steam\steamapps\common\Dyson Sphere Program",
    [string] $BepInExDirectory = (Join-Path $GameDirectory 'BepInEx')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectDirectory = $PSScriptRoot
$workspaceDirectory = $projectDirectory
$projectPath = Join-Path $projectDirectory 'GridOne.csproj'
$distDirectory = Join-Path $projectDirectory 'dist'
$temporaryDirectory = Join-Path $workspaceDirectory '.cache\tmp'
$environmentOverrides = @{
    DOTNET_CLI_HOME = (Join-Path $workspaceDirectory '.dotnet')
    NUGET_PACKAGES = (Join-Path $workspaceDirectory '.nuget\packages')
    TEMP = $temporaryDirectory
    TMP = $temporaryDirectory
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_NOLOGO = '1'
}
$originalEnvironment = @{}

try {
    foreach ($name in $environmentOverrides.Keys) {
        $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $environmentOverrides[$name], 'Process')
    }
    foreach ($directory in @($temporaryDirectory, $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $distDirectory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'The dotnet SDK is not on PATH. Install or enable the .NET 10 SDK before building.'
    }

    foreach ($entryName in @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'assets\icon-source.png')) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectDirectory $entryName) -PathType Leaf)) {
            throw "Missing package input: $entryName"
        }
    }

    Add-Type -AssemblyName System.Drawing
    $sourceIcon = [Drawing.Image]::FromFile((Join-Path $projectDirectory 'assets\icon-source.png'))
    try {
        if ($sourceIcon.Width -ne $sourceIcon.Height) {
            throw 'The source icon must be square to preserve its proportions.'
        }
        $icon = [Drawing.Bitmap]::new(256, 256)
        try {
            $graphics = [Drawing.Graphics]::FromImage($icon)
            try {
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($sourceIcon, 0, 0, 256, 256)
            }
            finally {
                $graphics.Dispose()
            }
            $icon.Save((Join-Path $projectDirectory 'icon.png'), [Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $icon.Dispose()
        }
    }
    finally {
        $sourceIcon.Dispose()
    }

    $buildArguments = @(
        'build', $projectPath,
        '--configuration', $Configuration,
        '--nologo',
        "-p:GameDirectory=$GameDirectory",
        "-p:BepInExDirectory=$BepInExDirectory"
    )
    & dotnet @buildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE. See the build errors above."
    }

    $outputDirectory = Join-Path $projectDirectory "bin\$Configuration\net472"
    Copy-Item -LiteralPath (Join-Path $outputDirectory 'GridOne.dll') -Destination $distDirectory -Force
    $symbolsPath = Join-Path $outputDirectory 'GridOne.pdb'
    if (Test-Path -LiteralPath $symbolsPath) {
        Copy-Item -LiteralPath $symbolsPath -Destination $distDirectory -Force
    }
    $readmePath = Join-Path $projectDirectory 'README.md'
    if (Test-Path -LiteralPath $readmePath) {
        Copy-Item -LiteralPath $readmePath -Destination $distDirectory -Force
    }
    Write-Host "Build complete: $(Join-Path $distDirectory 'GridOne.dll')"

    $manifestPath = Join-Path $projectDirectory 'manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $dllPath = Join-Path $distDirectory 'GridOne.dll'
    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dllPath).Version.ToString(3)
    if ($manifest.version_number -ne $assemblyVersion) {
        throw "Manifest version $($manifest.version_number) does not match assembly version $assemblyVersion."
    }

    $zipPath = Join-Path $distDirectory "$($manifest.name)-$assemblyVersion-Thunderstore.zip"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipStream = [IO.File]::Create($zipPath)
    try {
        $zip = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($entryName in @('manifest.json', 'README.md', 'icon.png', 'CHANGELOG.md', 'LICENSE')) {
                [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $projectDirectory $entryName), $entryName)
            }
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $dllPath, 'BepInEx/plugins/GridOne/GridOne.dll')
        }
        finally {
            $zip.Dispose()
        }
    }
    finally {
        $zipStream.Dispose()
    }
    Write-Host "Thunderstore package: $zipPath"
}
finally {
    foreach ($name in $originalEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name], 'Process')
    }
}
