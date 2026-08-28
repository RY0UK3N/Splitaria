param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "src\Splitaria.App\Splitaria.App.csproj"
$installerScript = Join-Path $projectRoot "installer\Splitaria.iss"
$publishDir = Join-Path $projectRoot "publish\installer-input"
$outputDir = Join-Path $projectRoot "publish\installer"

if ($Runtime -ne "win-x64") {
    throw "Este instalador está configurado somente para win-x64."
}

[xml]$project = Get-Content -Raw -LiteralPath $projectFile
$version = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "A versão não foi encontrada em $projectFile."
}

$compilerCandidates = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1

if (-not $compilerCandidates) {
    throw "Inno Setup 6 não foi encontrado. Instale-o e execute este script novamente."
}

if (Test-Path -LiteralPath $publishDir) {
    $resolvedPublishDir = (Resolve-Path -LiteralPath $publishDir).Path
    $expectedPublishDir = [IO.Path]::GetFullPath((Join-Path $projectRoot "publish\installer-input"))
    if ($resolvedPublishDir -ne $expectedPublishDir) {
        throw "A pasta de publicação não corresponde ao destino seguro esperado."
    }
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDir, $outputDir | Out-Null

dotnet publish $projectFile `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) { throw "A publicação do Splitaria falhou." }

# O pacote nativo do VLC contém três arquiteturas. O instalador é x64, portanto
# manter x86 e ARM64 apenas aumentaria o download sem poder ser usado pelo app.
$unusedVlcArchitectures = @(
    (Join-Path $publishDir "libvlc\win-x86"),
    (Join-Path $publishDir "libvlc\win-arm64")
)
foreach ($architecturePath in $unusedVlcArchitectures) {
    if (Test-Path -LiteralPath $architecturePath) {
        Remove-Item -LiteralPath $architecturePath -Recurse -Force
    }
}

& $compilerCandidates "/DMyAppVersion=$version" "/DPublishDir=$publishDir" "/DOutputDir=$outputDir" $installerScript
if ($LASTEXITCODE -ne 0) { throw "A criação do instalador falhou." }

$installer = Join-Path $outputDir "Splitaria-Setup-$version-win-x64.exe"
if (-not (Test-Path -LiteralPath $installer)) { throw "O instalador esperado não foi encontrado: $installer" }

$hash = Get-FileHash -LiteralPath $installer -Algorithm SHA256
Write-Host ""
Write-Host "Instalador criado: $installer"
Write-Host "SHA-256: $($hash.Hash)"
