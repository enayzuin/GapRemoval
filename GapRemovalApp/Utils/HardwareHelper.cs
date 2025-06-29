using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using Serilog;
using System.Linq;
using System.Windows.Controls;
using GapRemovalApp.Config;
using Xabe.FFmpeg;

namespace GapRemovalApp.Utils
{
    public static class HardwareHelper
    {
        public static readonly Dictionary<string, (string DisplayName, Dictionary<string, (string Preset, int Crf)>)> CodecDisplayMap =
            new()
            {
                ["libx264"] = (
                    "CPU (libx264)",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("veryslow", 6),
                        ["alta"] = ("slow", 16),
                        ["média"] = ("medium", 23),
                        ["baixa"] = ("ultrafast", 28)
                    }
                ),
                ["h264_nvenc"] = (
                    "NVIDIA NVENC H.264",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("hq", 6),
                        ["alta"] = ("slow", 16),
                        ["média"] = ("medium", 23),
                        ["baixa"] = ("fast", 28)
                    }
                ),
                ["hevc_nvenc"] = (
                    "NVIDIA NVENC HEVC",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("hq", 6),
                        ["alta"] = ("slow", 16),
                        ["média"] = ("medium", 23),
                        ["baixa"] = ("fast", 28)
                    }
                ),
                ["h264_qsv"] = (
                    "Intel Quick Sync Video",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("veryslow", 6),
                        ["alta"] = ("slow", 16),
                        ["média"] = ("medium", 23),
                        ["baixa"] = ("veryfast", 28)
                    }
                ),
                ["hevc_qsv"] = (
                    "Intel Quick Sync HEVC",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("veryslow", 6),
                        ["alta"] = ("slow", 16),
                        ["média"] = ("medium", 23),
                        ["baixa"] = ("veryfast", 28)
                    }
                ),
                ["h264_amf"] = (
                    "AMD AMF H.264",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("quality", 6),
                        ["alta"] = ("balanced", 16),
                        ["média"] = ("speed", 23),
                        ["baixa"] = ("speed", 28)
                    }
                ),
                ["hevc_amf"] = (
                    "AMD AMF HEVC",
                    new Dictionary<string, (string, int)>
                    {
                        ["muito alta"] = ("quality", 6),
                        ["alta"] = ("balanced", 16),
                        ["média"] = ("speed", 23),
                        ["baixa"] = ("speed", 28)
                    }
                )
            };

        public static Dictionary<string, string> GetHardwareAccelerationOptions()
        {
            var codecs = new Dictionary<string, string>
            {
                ["CPU (libx264)"] = "libx264" // sempre disponível
            };

            string gpuVendor = DetectGpuVendor();
            Log.Information("GPU detectada via WMI: {Gpu}", gpuVendor);

            foreach (var kvp in CodecDisplayMap)
            {
                string encoder = kvp.Key;
                string display = kvp.Value.DisplayName;

                if (encoder.Contains("nvenc") && gpuVendor == "nvidia")
                    codecs[display] = encoder;
                else if (encoder.Contains("qsv") && gpuVendor == "intel")
                    codecs[display] = encoder;
                else if (encoder.Contains("amf") && gpuVendor == "amd")
                    codecs[display] = encoder;
            }

            return codecs;
        }

        public static void PreencherComboBox(System.Windows.Controls.ComboBox comboBox, out Dictionary<string, string> map)
        {
            map = GetHardwareAccelerationOptions();
            comboBox.ItemsSource = map.Keys.ToList();
            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }

        private static string DetectGpuVendor()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString()?.ToLower() ?? "";
                    if (name.Contains("nvidia")) return "nvidia";
                    if (name.Contains("intel")) return "intel";
                    if (name.Contains("amd") || name.Contains("radeon")) return "amd";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao detectar GPU via WMI.");
            }

            return "desconhecido";
        }

        public static void ApplyCpuSettings(IConversion conversion, VideoProcessingConfig config)
        {
            conversion.AddParameter($"-c:v libx264");
            conversion.AddParameter($"-preset {config.Preset}");
            conversion.AddParameter($"-crf {config.Crf}");
        }

        public static void ApplyNvencSettings(IConversion conversion, VideoProcessingConfig config)
        {
            conversion.AddParameter($"-c:v {config.VideoEncoder}"); // ex: h264_nvenc
            conversion.AddParameter($"-preset {config.Preset}");
            conversion.AddParameter($"-rc vbr");
            conversion.AddParameter($"-cq {config.Crf}");
            conversion.AddParameter($"-b:v 0");
        }

        public static void ApplyQsvSettings(IConversion conversion, VideoProcessingConfig config)
        {
            conversion.AddParameter($"-c:v {config.VideoEncoder}"); // ex: h264_qsv
            conversion.AddParameter($"-preset {config.Preset}");
            conversion.AddParameter($"-global_quality {config.Crf}");
        }

        public static void ApplyAmfSettings(IConversion conversion, VideoProcessingConfig config)
        {
            conversion.AddParameter($"-c:v {config.VideoEncoder}"); // ex: h264_amf
            conversion.AddParameter($"-usage {config.Preset}");     // quality, balanced, speed
            conversion.AddParameter($"-rc vbr");
            conversion.AddParameter($"-qp {config.Crf}");
        }
    }
}
