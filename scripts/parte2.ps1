# Parte 2 do setup: Estrutura interna dos projetos e pastas adicionais
Write-Host '🔧 Iniciando Parte 2 do setup do projeto GapRemoval...'

# Pastas dentro de GapRemoval.Core
$coreDirs = @(
    'GapRemoval.Core/Models',
    'GapRemoval.Core/Services',
    'GapRemoval.Core/Utils'
)

# Pastas dentro de GapRemoval.FFmpeg
$ffmpegDirs = @(
    'GapRemoval.FFmpeg/Encoder',
    'GapRemoval.FFmpeg/Audio',
    'GapRemoval.FFmpeg/Video'
)

# Pastas dentro de GapRemoval.UI
$uiDirs = @(
    'GapRemoval.UI/Views',
    'GapRemoval.UI/ViewModels',
    'GapRemoval.UI/Components',
    'GapRemoval.UI/Resources'
)

# Criar diretórios
$allDirs = $coreDirs + $ffmpegDirs + $uiDirs

foreach ($dir in $allDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
        Write-Host ('📁 Criado: {0}' -f $dir)
    }
    else {
        Write-Host ('✅ Já existe: {0}' -f $dir)
    }
}

Write-Host ''
Write-Host '🚀 Parte 2 do setup concluída com sucesso!'