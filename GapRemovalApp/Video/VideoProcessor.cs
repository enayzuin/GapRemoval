using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace GapRemovalApp.Video
{
    public static class VideoProcessor
    {
        private const int MinSilenceLengthMs = 700;

    
        public static async Task<List<(TimeSpan start, TimeSpan end)>> OnlyDetectSilence(string videoPath, double silenceThresholdDb)
        {
            var silenceList = new List<(TimeSpan start, TimeSpan end)>();

            var process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = $"-i \"{videoPath}\" -af silencedetect=noise={silenceThresholdDb}dB:d=0.7 -f null -";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var silenceStartRegex = new Regex(@"silence_start: (?<start>[0-9.]+)", RegexOptions.Compiled);
            var silenceEndRegex = new Regex(@"silence_end: (?<end>[0-9.]+)", RegexOptions.Compiled);

            TimeSpan? currentSilenceStart = null;
            var tcs = new TaskCompletionSource();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    tcs.TrySetResult(); // fim da leitura
                    return;
                }

                var startMatch = silenceStartRegex.Match(e.Data);
                if (startMatch.Success)
                {
                    currentSilenceStart = TimeSpan.FromSeconds(double.Parse(startMatch.Groups["start"].Value, CultureInfo.InvariantCulture));
                }

                var endMatch = silenceEndRegex.Match(e.Data);
                if (endMatch.Success && currentSilenceStart.HasValue)
                {
                    var end = TimeSpan.FromSeconds(double.Parse(endMatch.Groups["end"].Value, CultureInfo.InvariantCulture));
                    silenceList.Add((currentSilenceStart.Value, end));
                    currentSilenceStart = null;
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            await Task.WhenAll(process.WaitForExitAsync(), tcs.Task);
            return silenceList;
        }


        public static async Task<List<(double start, double end)>> DetectSilence(string videoPath, int silenceThreshold)
        {
            var silentParts = new List<(double start, double end)>();

            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
                throw new ArgumentException("Caminho do vídeo inválido.");

            Console.WriteLine($"[FFmpeg] Detectando silêncio no vídeo: {videoPath} com threshold: {silenceThreshold}dB");

            string output = string.Empty;

            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i {videoPath}")
                .AddParameter($"-af silencedetect=n={silenceThreshold}dB:d=0.5")
                .AddParameter("-f null")
                .SetOutput("NUL"); // NUL no Windows, /dev/null no Linux/Mac

            conversion.OnDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    output += args.Data + Environment.NewLine;
            };

            await conversion.Start();

            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            double? silenceStart = null;

            foreach (string line in lines)
            {
                if (line.Contains("silence_start"))
                {
                    var value = line.Split("silence_start:").LastOrDefault()?.Trim();
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double start))
                        silenceStart = start;
                }

                if (line.Contains("silence_end") && silenceStart.HasValue)
                {
                    var value = line.Split("silence_end:").LastOrDefault()?.Split(' ').FirstOrDefault()?.Trim();
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double end))
                    {
                        double duration = (end - silenceStart.Value) * 1000;
                        if (duration >= MinSilenceLengthMs)
                            silentParts.Add((silenceStart.Value, end));

                        silenceStart = null;
                    }
                }
            }

            Console.WriteLine($"[FFmpeg] Detecção concluída. {silentParts.Count} trechos silenciosos encontrados.");
            return silentParts;
        }

        public static async Task<List<string>> CutVideo(string videoPath, List<(TimeSpan start, TimeSpan end)> silentParts)
        {
            var parts = new List<string>();
            TimeSpan lastEnd = TimeSpan.Zero;

            foreach (var (start, end) in silentParts)
            {
                if (start > lastEnd)
                {
                    string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");

                    Console.WriteLine($"[FFmpeg] Cortando trecho de {lastEnd} até {start} → {tempFile}");

                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-ss {lastEnd}")
                        .AddParameter($"-to {start}")
                        .AddParameter($"-i \"{videoPath}\"")
                        .AddParameter("-avoid_negative_ts make_zero")
                        .AddParameter("-c:v libx264")
                        .AddParameter("-preset slow")
                        .AddParameter("-crf 16")
                        .AddParameter("-c:a aac")
                        .SetOutput(tempFile);

                    await conversion.Start();
                    parts.Add(tempFile);
                }

                lastEnd = end;
            }

            Console.WriteLine($"[FFmpeg] {parts.Count} partes de vídeo cortadas com sucesso.");
            return parts;
        }


        public static async Task ConcatenateVideos(string videoPath, List<string> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                Console.WriteLine("❌ Nenhuma parte para concatenar.");
                return;
            }

            string outputPath = Path.Combine(
                Path.GetDirectoryName(videoPath)!,
                Path.GetFileNameWithoutExtension(videoPath) + "_sem_silencio.mp4"
            );

            string listFilePath = Path.Combine(Path.GetTempPath(), $"concat_list_{Guid.NewGuid():N}.txt");

            using (var writer = new StreamWriter(listFilePath))
            {
                foreach (var part in parts)
                {
                    writer.WriteLine($"file '{part.Replace("\\", "/")}'");
                }
            }

            Console.WriteLine($"[FFmpeg] Iniciando concatenação para: {outputPath}");

            try
            {
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter("-f concat")
                    .AddParameter("-safe 0")
                    .AddParameter($"-i {listFilePath}")
                    .AddParameter("-c copy")
                    .SetOutput(outputPath);

                await conversion.Start();

                Console.WriteLine($"✅ Vídeo final gerado em: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao concatenar: {ex.Message}");
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
    }
}