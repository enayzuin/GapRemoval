# Script para download de binários FFmpeg e VLC usando winget
param(
    [string]$ProjectPath = $PSScriptRoot
)

Write-Host "Iniciando instalação de binários via winget..." -ForegroundColor Green

# Criar diretórios se não existirem
$ffmpegDir = Join-Path -Path $ProjectPath -ChildPath "ffmpeg"
$vlcDir = Join-Path -Path $ProjectPath -ChildPath "vlc"

if (!(Test-Path $ffmpegDir)) {
    New-Item -ItemType Directory -Path $ffmpegDir -Force | Out-Null
    Write-Host "Diretório ffmpeg criado." -ForegroundColor Yellow
}

if (!(Test-Path $vlcDir)) {
    New-Item -ItemType Directory -Path $vlcDir -Force | Out-Null
    Write-Host "Diretório vlc criado." -ForegroundColor Yellow
}

# Verificar se winget está disponível
try {
    $wingetVersion = winget --version
    Write-Host "Winget encontrado: $wingetVersion" -ForegroundColor Green
} catch {
    Write-Host "Erro: Winget não está disponível. Instale o App Installer do Microsoft Store." -ForegroundColor Red
    exit 1
}

# Instalar/copiar FFmpeg se não existir
if (!(Test-Path "$ffmpegDir\ffmpeg.exe")) {
    Write-Host "Instalando FFmpeg via winget..." -ForegroundColor Yellow
    
    try {
        # Instalar FFmpeg via winget
        winget install Gyan.FFmpeg --accept-source-agreements --accept-package-agreements
        
        # Procurar onde o FFmpeg foi instalado
        $ffmpegPaths = @(
            "${env:ProgramFiles}\ffmpeg\bin",
            "${env:ProgramFiles(x86)}\ffmpeg\bin",
            "${env:LOCALAPPDATA}\Microsoft\WinGet\Packages\Gyan.FFmpeg_*",
            "${env:ProgramFiles}\WindowsApps\Gyan.FFmpeg_*"
        )
        
        $ffmpegFound = $false
        foreach ($path in $ffmpegPaths) {
            $expandedPath = [System.Environment]::ExpandEnvironmentVariables($path)
            if (Test-Path "$expandedPath\ffmpeg.exe") {
                Copy-Item -Path "$expandedPath\*" -Destination $ffmpegDir -Force
                $ffmpegFound = $true
                Write-Host "FFmpeg copiado de: $expandedPath" -ForegroundColor Green
                break
            }
        }
        
        if (!$ffmpegFound) {
            # Tentar encontrar via PATH
            $ffmpegInPath = Get-Command ffmpeg -ErrorAction SilentlyContinue
            if ($ffmpegInPath) {
                $ffmpegDirPath = Split-Path -Parent $ffmpegInPath.Source
                Copy-Item -Path "$ffmpegDirPath\*" -Destination $ffmpegDir -Force
                Write-Host "FFmpeg copiado do PATH: $ffmpegDirPath" -ForegroundColor Green
            } else {
                Write-Host "FFmpeg instalado mas não encontrado. Tentando download manual..." -ForegroundColor Yellow
                # Fallback para download manual
                $ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
                $tempZip = Join-Path -Path $env:TEMP -ChildPath "ffmpeg.zip"
                
                Invoke-WebRequest -Uri $ffmpegUrl -OutFile $tempZip
                Expand-Archive -Path $tempZip -DestinationPath $env:TEMP -Force
                
                $extractedDir = Get-ChildItem -Path $env:TEMP -Directory | Where-Object { $_.Name -like "*ffmpeg*" } | Select-Object -First 1
                if ($extractedDir) {
                    Copy-Item -Path "$($extractedDir.FullName)\bin\*" -Destination $ffmpegDir -Force
                }
                
                Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
                Remove-Item -Path $extractedDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        
        Write-Host "FFmpeg instalado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "Erro ao instalar FFmpeg: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "FFmpeg já existe." -ForegroundColor Green
}

# Instalar/copiar VLC se não existir
if (!(Test-Path "$vlcDir\libvlc.dll")) {
    Write-Host "Instalando VLC via winget..." -ForegroundColor Yellow
    
    try {
        # Instalar VLC via winget
        winget install VideoLAN.VLC --accept-source-agreements --accept-package-agreements
        
        # Procurar onde o VLC foi instalado
        $vlcPaths = @(
            "${env:ProgramFiles}\VideoLAN\VLC",
            "${env:ProgramFiles(x86)}\VideoLAN\VLC",
            "${env:LOCALAPPDATA}\Microsoft\WinGet\Packages\VideoLAN.VLC_*",
            "${env:ProgramFiles}\WindowsApps\VideoLAN.VLC_*"
        )
        
        $vlcFound = $false
        foreach ($path in $vlcPaths) {
            $expandedPath = [System.Environment]::ExpandEnvironmentVariables($path)
            if (Test-Path "$expandedPath\libvlc.dll") {
                # Copiar DLLs principais
                Copy-Item -Path "$expandedPath\libvlc.dll" -Destination $vlcDir -Force
                Copy-Item -Path "$expandedPath\libvlccore.dll" -Destination $vlcDir -Force
                # Copiar a pasta plugins
                if (Test-Path "$expandedPath\plugins") {
                    Copy-Item -Path "$expandedPath\plugins" -Destination $vlcDir -Recurse -Force
                }
                # Copiar outras DLLs importantes (opcional)
                $vlcFound = $true
                Write-Host "VLC copiado de: $expandedPath" -ForegroundColor Green
                break
            }
        }
        
        if (!$vlcFound) {
            Write-Host "VLC instalado mas não encontrado. Tentando download manual..." -ForegroundColor Yellow
            # Fallback para download manual
            $vlcUrl = "https://get.videolan.org/vlc/3.0.18/win64/vlc-3.0.18-win64.zip"
            $tempZip = Join-Path -Path $env:TEMP -ChildPath "vlc.zip"
            
            Invoke-WebRequest -Uri $vlcUrl -OutFile $tempZip
            Expand-Archive -Path $tempZip -DestinationPath $env:TEMP -Force
            
            $extractedDir = Get-ChildItem -Path $env:TEMP -Directory | Where-Object { $_.Name -like "*vlc*" } | Select-Object -First 1
            if ($extractedDir) {
                Copy-Item -Path "$($extractedDir.FullName)\*" -Destination $vlcDir -Recurse -Force
            }
            
            Remove-Item -Path $tempZip -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $extractedDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
        
        Write-Host "VLC instalado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "Erro ao instalar VLC: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "VLC já existe." -ForegroundColor Green
}

Write-Host "Instalação de binários concluída!" -ForegroundColor Green 