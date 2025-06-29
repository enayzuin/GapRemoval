# PowerShell Script: build_and_publish.ps1
# Requisitos: PowerShell 5.1+

# Variáveis
$projectPath = "D:\devzuin\dev_pessoal\gap removal\GapRemovalApp"
$configuration = "Release"
$runtime = "win-x64"
$outputDir = Join-Path -Path $PSScriptRoot -ChildPath "bin\Release\net8.0-windows\$runtime\publish"
$issScript = "GapRemoval.iss"
$issExecuteScript = Join-Path -Path $projectPath -ChildPath $issScript

Write-Host ""
Write-Host "Projeto: $projectPath"
Write-Host "Configuracao: $configuration"

# Etapa 1 - Limpeza
Write-Host ""
Write-Host "-------------------------"
Write-Host "1. Limpando build antigo"
Write-Host "-------------------------"
dotnet clean $projectPath --configuration $configuration
Remove-Item -Recurse -Force $outputDir

# Etapa 2 - Publicacao
Write-Host ""
Write-Host "-------------------------"
Write-Host "2. Publicando aplicacao"
Write-Host "-------------------------"
dotnet publish $projectPath --configuration $configuration --runtime $runtime --self-contained `
    /p:PublishSingleFile=false /p:PublishTrimmed=false /p:PublishReadyToRun=false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Erro ao fazer publish. Abortando..."
    Pause
    exit $LASTEXITCODE
}

# Etapa 3 - Instalador
Write-Host ""
Write-Host "-------------------------"
Write-Host "3. Executando Inno Setup"
Write-Host "-------------------------"
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" $issExecuteScript

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Erro ao gerar instalador."
    Pause
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Build e instalacao concluidos com sucesso."
Pause
