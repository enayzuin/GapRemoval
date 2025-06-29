using System.IO;

namespace GapRemovalApp.Config
{
    public class VideoProcessingConfig
    {
        public string? Preset { get; set; }           // Ex: "slow", "medium", "ultrafast"
        public int Crf { get; set; }                  // Ex: 16, 23, 28
        public int Threads { get; set; }              // Ex: 0 (auto) ou número de threads
        public int? TargetFps { get; set; }           // Ex: 30, 60, ou null para manter original
        public string? VideoEncoder { get; set; }     // Ex: "libx264", "h264_nvenc", etc

        public int QualityLevelIndex { get; set; }

        public static string GetConfigPath()
        {
            // Define a pasta: C:\Users\<usuário>\AppData\Roaming\GapRemovalApp\config
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GapRemovalApp",
                "config");

            // Cria a pasta se ainda não existir
            Directory.CreateDirectory(folder);

            // Retorna o caminho completo do JSON
            return Path.Combine(folder, "video_settings.json");
        }
    }
}
