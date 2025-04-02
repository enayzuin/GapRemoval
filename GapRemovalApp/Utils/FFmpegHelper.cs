using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GapRemovalApp.Utils
{
    public static class FFmpegHelper
    {
        public static async Task<string> ConverterParaCompatibilidade(string inputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Arquivo de vídeo não encontrado", inputPath);

            string pastaTemp = Path.Combine(Path.GetTempPath(), "GapRemovalConverted");
            Directory.CreateDirectory(pastaTemp);

            string nomeArquivo = $"video_{Guid.NewGuid()}.mp4";
            string outputPath = Path.Combine(pastaTemp, nomeArquivo);

            // ⚠️ COMANDO ATUALIZADO para máxima compatibilidade
            string argumentos = $"-i \"{inputPath}\" -c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p -c:a aac -movflags +faststart -y \"{outputPath}\"";

            var processo = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = argumentos,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            try
            {
                processo.Start();

                // Captura a saída do FFmpeg para debug
                string stderr = await processo.StandardError.ReadToEndAsync();
                await processo.WaitForExitAsync();

                if (processo.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length < 1000)
                {
                    throw new Exception("FFmpeg falhou ou gerou arquivo inválido:\n" + stderr);
                }

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao executar FFmpeg:\n" + ex.Message);
            }
        }
    }
}