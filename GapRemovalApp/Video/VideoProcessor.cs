using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Xabe.FFmpeg;
using GapRemovalApp.Utils;
using GapRemovalApp.Config;
using MessageBox = System.Windows.MessageBox;

namespace GapRemovalApp.Video
{
    public static class VideoProcessor
    {
        private const int MinSilenceLengthMs = 700;

        public static async Task<List<string>> CutVideo(string videoPath, List<(TimeSpan start, TimeSpan end)> silentParts, Action<double>? onProgress = null)
        {
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
            FFmpeg.SetExecutablesPath(ffmpegPath);

            var parts = new List<string>();
            var (width, height) = await GetVideoResolution(videoPath);

            Logger.Info($"[Video Info] Resolução detectada: {width}x{height}");

            double totalDurationToProcess = CalculateTotalDuration(silentParts);
            double processedSoFar = 0.0;
            TimeSpan lastEnd = TimeSpan.Zero;

            foreach (var (start, end) in silentParts)
            {
                if (start > lastEnd)
                {
                    var segmentPath = await CutSegment(videoPath, lastEnd, start, width, totalDurationToProcess, processedSoFar, (value) =>
                    {
                        onProgress?.Invoke(value);
                    });

                    parts.Add(segmentPath);
                    processedSoFar += (start - lastEnd).TotalSeconds;
                }

                lastEnd = end;
            }

            Logger.Info($"[FFmpeg] {parts.Count} partes de vídeo cortadas com sucesso.");
            return parts;
        }

        private static async Task<(int width, int height)> GetVideoResolution(string videoPath)
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            int width = videoStream?.Width ?? 1920;
            int height = videoStream?.Height ?? 1080;
            return (width, height);
        }

        private static double CalculateTotalDuration(List<(TimeSpan start, TimeSpan end)> silentParts)
        {
            double total = 0.0;
            TimeSpan lastEnd = TimeSpan.Zero;

            foreach (var (start, end) in silentParts)
            {
                if (start > lastEnd)
                {
                    total += (start - lastEnd).TotalSeconds;
                }
                lastEnd = end;
            }

            return total;
        }

        private static async Task<string> CutSegment(string videoPath, TimeSpan start, TimeSpan end, int width, double totalDuration, double processedSoFar, Action<double>? onProgress)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");

            Logger.Info($"[FFmpeg] Cortando trecho de {start} até {end} → {tempFile}");

            var config = LoadProcessingConfig();
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-ss {start}")
                .AddParameter($"-to {end}")
                .AddParameter($"-i \"{videoPath}\"")
                .AddParameter("-avoid_negative_ts make_zero")
                .AddParameter("-pix_fmt yuv420p");

            string encoder = config?.VideoEncoder ?? "libx264";

            switch (encoder)
            {
                case "libx264":
                    HardwareHelper.ApplyCpuSettings(conversion, config);
                    break;

                case "h264_nvenc":
                case "hevc_nvenc":
                    HardwareHelper.ApplyNvencSettings(conversion, config);
                    break;

                case "h264_qsv":
                case "hevc_qsv":
                    HardwareHelper.ApplyQsvSettings(conversion, config);
                    break;

                case "h264_amf":
                case "hevc_amf":
                    HardwareHelper.ApplyAmfSettings(conversion, config);
                    break;

                default:
                    Logger.Warn($"Encoder desconhecido '{encoder}', aplicando fallback para CPU.");
                    HardwareHelper.ApplyCpuSettings(conversion, config);
                    break;
            }

            if (config?.TargetFps != null)
                conversion.AddParameter($"-r {config.TargetFps}");

            conversion.AddParameter("-c:a aac");
            conversion.AddParameter($"-threads {config?.Threads ?? 0}");
            conversion.SetOutput(tempFile);

            TimeSpan clipDuration = end - start;

            conversion.OnDataReceived += (sender, args) =>
            {
                Logger.Info($"[FFmpeg Output] {args.Data}");
            };

            conversion.OnProgress += (sender, args) =>
            {
                if (clipDuration.TotalSeconds > 0)
                {
                    double currentClipSeconds = args.Duration.TotalSeconds;
                    double globalSeconds = processedSoFar + currentClipSeconds;
                    double globalPercent = (globalSeconds / totalDuration) * 100.0;
                    Logger.Debug($"[FFmpeg Progress] Global: {globalPercent:F1}%");
                    onProgress?.Invoke(globalPercent);
                }
            };

            await conversion.Start();
            Logger.Info("Conversão concluída com sucesso.");

            return tempFile;
        }


        public static async Task ConcatenateVideos(string outputPath, List<string> parts)
        {
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
            FFmpeg.SetExecutablesPath(ffmpegPath);

            if (parts == null || parts.Count == 0)
            {
                Logger.Warn("Nenhuma parte para concatenar.");
                return;
            }

            string listFilePath = Path.Combine(Path.GetTempPath(), $"concat_list_{Guid.NewGuid():N}.txt");
            await File.WriteAllLinesAsync(listFilePath, parts.Select(p => $"file '{p.Replace("\\", "/")}'"));

            Logger.Info($"[FFmpeg] Iniciando concatenação para: {outputPath}");

            try
            {
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter("-f concat")
                    .AddParameter("-safe 0")
                    .AddParameter($"-i \"{listFilePath}\"")
                    .AddParameter("-c copy")
                    .SetOutput(outputPath);

                await conversion.Start();

                Logger.Info($"✅ Vídeo final gerado em: {outputPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Erro ao concatenar: {ex.Message}");
            }
            finally
            {
                try { File.Delete(listFilePath); } catch { }

                foreach (var part in parts)
                {
                    try { File.Delete(part); } catch { }
                }
            }
        }

        private static VideoProcessingConfig? LoadProcessingConfig()
        {
            try
            {
                string configPath = VideoProcessingConfig.GetConfigPath();
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<VideoProcessingConfig>(json);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Falha ao carregar configuração: {ex.Message}");
            }

            return JsonSerializer.Deserialize<VideoProcessingConfig>("");
        }




    }
}
