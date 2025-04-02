using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using NAudio.Wave;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace GapRemovalApp.Utils
{
    public static class Audio
    {
        public static async Task<string> ExtrairWav(string videoPath)
        {
            string pastaTemp = Path.Combine(Path.GetTempPath(), "GapRemovalWaveform");
            Directory.CreateDirectory(pastaTemp);

            string outputWav = Path.Combine(pastaTemp, $"audio_{Guid.NewGuid()}.wav");

            string argumentos = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 44100 -ac 1 -y \"{outputWav}\"";

            var processo = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = argumentos,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            try
            {
                processo.Start();
                await processo.StandardError.ReadToEndAsync();
                await processo.WaitForExitAsync();

                if (!File.Exists(outputWav))
                    throw new Exception("Falha ao extrair áudio com FFmpeg");
                return outputWav;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao extrair áudio:\n" + ex.Message);
            }
        }

                   public static List<(TimeSpan start, TimeSpan end)> DetectarSilencios(string wavPath, float thresholdDb = -40f, int minSilenceMs = 700)
            {
                var silentParts = new List<(TimeSpan, TimeSpan)>();

                using var reader = new AudioFileReader(wavPath);
                int sampleRate = reader.WaveFormat.SampleRate;
                int channels = reader.WaveFormat.Channels;
                float[] buffer = new float[reader.Length / sizeof(float)];
                reader.Read(buffer, 0, buffer.Length);

                int samplesPerMs = sampleRate / 1000;
                int minSilentSamples = minSilenceMs * samplesPerMs * channels;

                bool inSilence = false;
                int silenceStart = 0;

                for (int i = 0; i < buffer.Length; i += channels)
                {
                    float maxAmplitude = 0f;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        float sample = Math.Abs(buffer[i + ch]);
                        maxAmplitude = Math.Max(maxAmplitude, sample);
                    }

                    float db = 20 * MathF.Log10(maxAmplitude + 1e-10f);

                    if (db < thresholdDb)
                    {
                        if (!inSilence)
                        {
                            inSilence = true;
                            silenceStart = i;
                        }
                    }
                    else if (inSilence)
                    {
                        int silenceLength = i - silenceStart;
                        if (silenceLength >= minSilentSamples)
                        {
                            TimeSpan start = TimeSpan.FromSeconds((double)silenceStart / sampleRate / channels);
                            TimeSpan end = TimeSpan.FromSeconds((double)i / sampleRate / channels);
                            silentParts.Add((start, end));
                        }
                        inSilence = false;
                    }
                }

                return silentParts;
            }
        }
}
