# Parte 4.1 do setup: Criação da classe VideoProcessor
Write-Host '`n🎞️ Criando classe VideoProcessor...'

# Criar a pasta se não existir
$videoFolder = './GapRemovalApp/Video'
if (!(Test-Path $videoFolder)) {
    New-Item -ItemType Directory -Path $videoFolder | Out-Null
}

# Criar o arquivo VideoProcessor.cs
@'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using NAudio.Wave;

namespace GapRemovalApp.Video
{
    public class VideoProcessor
    {
        public string VideoPath { get; }
        public string OutputPath { get; }
        public int SilenceThreshold { get; }
        public int MinSilenceLengthMs { get; } = 700;

        public VideoProcessor(string videoPath, string outputPath, int silenceThreshold)
        {
            VideoPath = videoPath;
            OutputPath = outputPath;
            SilenceThreshold = silenceThreshold;
        }

        public async Task<List<(double start, double end)>> DetectSilence()
        {
            // TODO: Implementar detecção de silêncio com NAudio
            return new List<(double, double)>();
        }

        public async Task<List<string>> CutVideo(List<(double start, double end)> silentParts)
        {
            // TODO: Cortar vídeo com base nas partes não silenciosas usando Xabe.FFmpeg
            return new List<string>();
        }

        public async Task ConcatenateVideos(List<string> parts)
        {
            // TODO: Concatenar partes com Xabe.FFmpeg
        }

        public async Task RemoveSilence()
        {
            var silentParts = await DetectSilence();
            if (silentParts.Count == 0)
            {
                File.Copy(VideoPath, OutputPath, overwrite: true);
                Console.WriteLine("Nenhum silêncio detectado. Vídeo copiado sem alterações.");
                return;
            }

            var parts = await CutVideo(silentParts);
            await ConcatenateVideos(parts);

            // Limpeza temporária
            foreach (var file in parts)
            {
                try { File.Delete(file); } catch { }
            }
        }
    }
}
'@ | Set-Content -Path "$videoFolder/VideoProcessor.cs" -Encoding utf8

Write-Host '`n✅ Parte 4.1 concluída com sucesso!'