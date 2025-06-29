using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using NAudio.Wave;
using Xabe.FFmpeg;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace GapRemovalApp.Utils
{
    public static class Audio
    {
        public static async Task<string> ExtrairWav(string videoPath)
        {
            try
            {
                // Verificar se o arquivo de vídeo existe
                if (!File.Exists(videoPath))
                {
                    throw new FileNotFoundException($"Arquivo de vídeo não encontrado: {videoPath}");
                }

                var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
                
                // Verificar se o FFmpeg existe
                var ffmpegExe = Path.Combine(ffmpegPath, "ffmpeg.exe");
                if (!File.Exists(ffmpegExe))
                {
                    throw new FileNotFoundException($"FFmpeg não encontrado em: {ffmpegExe}");
                }

                FFmpeg.SetExecutablesPath(ffmpegPath);
                
                string pastaTemp = Path.Combine(Path.GetTempPath(), "GapRemovalWaveform");
                Directory.CreateDirectory(pastaTemp);

                string outputWav = Path.Combine(pastaTemp, $"audio_{Guid.NewGuid()}.wav");

                System.Windows.MessageBox.Show($"Iniciando extração de áudio...\nVídeo: {videoPath}\nSaída: {outputWav}", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                IConversion conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{videoPath}\"")
                    .AddParameter("-vn") // Sem vídeo
                    .AddParameter("-acodec pcm_s16le") // Formato WAV
                    .AddParameter("-ar 44100") // Sample rate
                    .AddParameter("-ac 1") // Mono
                    .SetOutput(outputWav);

                await conversion.Start();

                // Verificar se o arquivo foi criado e tem tamanho > 0
                if (!File.Exists(outputWav))
                {
                    throw new Exception("Falha ao extrair áudio: arquivo não foi criado");
                }

                var fileInfo = new FileInfo(outputWav);
                if (fileInfo.Length == 0)
                {
                    throw new Exception("Falha ao extrair áudio: arquivo criado mas está vazio");
                }

                System.Windows.MessageBox.Show($"Áudio extraído com sucesso!\nTamanho: {fileInfo.Length} bytes\nCaminho: {outputWav}", "Sucesso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                return outputWav;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao extrair áudio:\n{ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw new Exception("Erro ao extrair áudio:\n" + ex.Message);
            }
        }

        public static List<(TimeSpan start, TimeSpan end)> DetectarSilencios(string wavPath, float thresholdDb = -40f, int minSilenceMs = 700)
        {
            var silentParts = new List<(TimeSpan start, TimeSpan end)>();
            const int minSpeechMs = 500; // Trechos com menos de 500ms serão absorvidos como silêncio

            using var reader = new AudioFileReader(wavPath);
            int sampleRate = reader.WaveFormat.SampleRate;
            int channels = reader.WaveFormat.Channels;
            int blockSize = 1024;

            float[] buffer = new float[blockSize * channels];
            int read;
            long totalSamplesRead = 0;

            bool inSilence = false;
            long silenceStartSample = 0;
            int minSilentSamples = minSilenceMs * sampleRate / 1000;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i += channels)
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
                            silenceStartSample = totalSamplesRead;
                        }
                    }
                    else if (inSilence)
                    {
                        long silenceEndSample = totalSamplesRead;
                        if ((silenceEndSample - silenceStartSample) >= minSilentSamples)
                        {
                            TimeSpan start = TimeSpan.FromSeconds((double)silenceStartSample / sampleRate);
                            TimeSpan end = TimeSpan.FromSeconds((double)silenceEndSample / sampleRate);
                            silentParts.Add((start, end));
                        }
                        inSilence = false;
                    }

                    totalSamplesRead++;
                }
            }

            // Fusão de trechos muito curtos de fala (menos de 500ms entre silêncios)
            var resultadoFinal = new List<(TimeSpan start, TimeSpan end)>();

            for (int i = 0; i < silentParts.Count; i++)
            {
                if (i == 0)
                {
                    resultadoFinal.Add(silentParts[i]);
                    continue;
                }

                var anterior = resultadoFinal[^1];
                var atual = silentParts[i];

                var speechDuration = atual.start - anterior.end;

                if (speechDuration.TotalMilliseconds < minSpeechMs)
                {
                    // Absorve o trecho de fala curto, unindo os dois silêncios
                    resultadoFinal[^1] = (anterior.start, atual.end);
                }
                else
                {
                    resultadoFinal.Add(atual);
                }
            }

            return resultadoFinal;
        }




    }
}
