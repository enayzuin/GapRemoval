# PowerShell Script: build_distribution.ps1
# Script completo para build e distribuição do Gap Removal
# Requisitos: PowerShell 5.1+, .NET 8.0, Inno Setup 6

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipDownload = $false
)

# Variáveis
$projectPath = $PSScriptRoot
$outputDir = Join-Path -Path $projectPath -ChildPath "bin\$Configuration\net8.0-windows\$Runtime\publish"
$issScript = "GapRemoval.iss"
$ffmpegDir = Join-Path -Path $projectPath -ChildPath "ffmpeg"
$vlcDir = Join-Path -Path $projectPath -ChildPath "vlc"

Write-Host ""
Write-Host "=========================================="
Write-Host "  Gap Removal - Build e Distribuição"
Write-Host "=========================================="
Write-Host "Projeto: $projectPath"
Write-Host "Configuração: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host ""

# Função para baixar FFmpeg
function Download-FFmpeg {
    Write-Host "Baixando FFmpeg..."
    
    # Criar diretório se não existir
    if (!(Test-Path $ffmpegDir)) {
        New-Item -ItemType Directory -Path $ffmpegDir -Force | Out-Null
    }
    
    # URLs do FFmpeg (versão estática)
    $ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
    $tempZip = Join-Path -Path $env:TEMP -ChildPath "ffmpeg.zip"
    
    try {
        Write-Host "Downloading FFmpeg..."
        Invoke-WebRequest -Uri $ffmpegUrl -OutFile $tempZip
        
        Write-Host "Extraindo FFmpeg..."
        Expand-Archive -Path $tempZip -DestinationPath $env:TEMP -Force
        
        # Mover arquivos para a pasta ffmpeg
        $extractedDir = Get-ChildItem -Path $env:TEMP -Directory | Where-Object { $_.Name -like "*ffmpeg*" } | Select-Object -First 1
        if ($extractedDir) {
            Copy-Item -Path "$($extractedDir.FullName)\bin\*" -Destination $ffmpegDir -Force
        }
        
        # Limpar arquivos temporários
        Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $extractedDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        
        Write-Host "FFmpeg baixado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "Erro ao baixar FFmpeg: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Por favor, baixe manualmente de https://ffmpeg.org/download.html" -ForegroundColor Yellow
        return $false
    }
    
    return $true
}

# Função para baixar VLC
function Download-VLC {
    Write-Host "Baixando VLC..."
    
    # Criar diretório se não existir
    if (!(Test-Path $vlcDir)) {
        New-Item -ItemType Directory -Path $vlcDir -Force | Out-Null
    }
    
    # URL do VLC
    $vlcUrl = "https://get.videolan.org/vlc/last/win64/vlc-3.0.18-win64.zip"
    $tempZip = Join-Path -Path $env:TEMP -ChildPath "vlc.zip"
    
    try {
        Write-Host "Downloading VLC..."
        Invoke-WebRequest -Uri $vlcUrl -OutFile $tempZip
        
        Write-Host "Extraindo VLC..."
        Expand-Archive -Path $tempZip -DestinationPath $env:TEMP -Force
        
        # Mover arquivos para a pasta vlc
        $extractedDir = Get-ChildItem -Path $env:TEMP -Directory | Where-Object { $_.Name -like "*vlc*" } | Select-Object -First 1
        if ($extractedDir) {
            Copy-Item -Path "$($extractedDir.FullName)\*" -Destination $vlcDir -Recurse -Force
        }
        
        # Limpar arquivos temporários
        Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $extractedDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        
        Write-Host "VLC baixado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "Erro ao baixar VLC: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Por favor, baixe manualmente de https://www.videolan.org/vlc/" -ForegroundColor Yellow
        return $false
    }
    
    return $true
}

# Verificar se os binários existem
if (!$SkipDownload) {
    Write-Host "-------------------------"
    Write-Host "Verificando binários..."
    Write-Host "-------------------------"
    
    $ffmpegExists = (Test-Path "$ffmpegDir\ffmpeg.exe") -and (Test-Path "$ffmpegDir\ffplay.exe") -and (Test-Path "$ffmpegDir\ffprobe.exe")
    $vlcExists = (Test-Path "$vlcDir\libvlc.dll") -and (Test-Path "$vlcDir\libvlccore.dll")
    
    if (!$ffmpegExists) {
        Write-Host "FFmpeg não encontrado. Baixando..." -ForegroundColor Yellow
        Download-FFmpeg
    } else {
        Write-Host "FFmpeg encontrado." -ForegroundColor Green
    }
    
    if (!$vlcExists) {
        Write-Host "VLC não encontrado. Baixando..." -ForegroundColor Yellow
        Download-VLC
    } else {
        Write-Host "VLC encontrado." -ForegroundColor Green
    }
}

# Etapa 1 - Limpeza
Write-Host ""
Write-Host "-------------------------"
Write-Host "1. Limpando build antigo"
Write-Host "-------------------------"
dotnet clean $projectPath --configuration $Configuration
if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}

# Etapa 2 - Publicação
Write-Host ""
Write-Host "-------------------------"
Write-Host "2. Publicando aplicação"
Write-Host "-------------------------"
dotnet publish $projectPath --configuration $Configuration --runtime $Runtime --self-contained `
    /p:PublishSingleFile=false /p:PublishTrimmed=false /p:PublishReadyToRun=false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Erro ao fazer publish. Abortando..." -ForegroundColor Red
    Pause
    exit $LASTEXITCODE
}

# Etapa 3 - Verificar se Inno Setup está instalado
Write-Host ""
Write-Host "-------------------------"
Write-Host "3. Verificando Inno Setup"
Write-Host "-------------------------"
$innoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (!(Test-Path $innoSetupPath)) {
    Write-Host "Inno Setup não encontrado em: $innoSetupPath" -ForegroundColor Red
    Write-Host "Por favor, instale o Inno Setup 6 de: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Pause
    exit 1
}

# Etapa 4 - Gerar Instalador
Write-Host ""
Write-Host "-------------------------"
Write-Host "4. Gerando instalador"
Write-Host "-------------------------"
& $innoSetupPath $issScript

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Erro ao gerar instalador." -ForegroundColor Red
    Pause
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=========================================="
Write-Host "  Build e distribuição concluídos!"
Write-Host "=========================================="
Write-Host "Instalador gerado em: $projectPath\Output\GapRemovalInstaller.exe" -ForegroundColor Green
Write-Host ""
Pause 