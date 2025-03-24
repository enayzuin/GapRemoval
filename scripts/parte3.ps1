# Parte 3 do setup: Geração de arquivos de exemplo
Write-Host '📄 Gerando arquivos iniciais de exemplo...'

$coreFile = 'GapRemoval.Core/Utils/Logger.cs'
$ffmpegFile = 'GapRemoval.FFmpeg/Video/VideoCutter.cs'
$uiFile = 'GapRemoval.UI/Views/MainWindow.xaml'

# Criar arquivo de exemplo em Core
@'
using System;
namespace GapRemoval.Core.Utils {
    public static class Logger {
        public static void Info(string message) {
            Console.WriteLine($"[INFO] {message}");
        }
    }
}
'@ | Set-Content -Path $coreFile -Encoding utf8

# Criar arquivo de exemplo em FFmpeg
@'
using System;
namespace GapRemoval.FFmpeg.Video {
    public class VideoCutter {
        public void Cut(string inputPath, string outputPath) {
            Console.WriteLine($"Cutting video: {inputPath} -> {outputPath}");
        }
    }
}
'@ | Set-Content -Path $ffmpegFile -Encoding utf8

# Criar arquivo XAML em UI
@'
<Window x:Class="GapRemoval.UI.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="GapRemoval" Height="350" Width="525">
    <Grid>
        <TextBlock Text="Olá, GapRemoval!" HorizontalAlignment="Center" VerticalAlignment="Center" FontSize="24"/>
    </Grid>
</Window>
'@ | Set-Content -Path $uiFile -Encoding utf8

Write-Host "`n✅ Parte 3 concluída com sucesso!"