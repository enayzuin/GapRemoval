using GapRemovalApp.Config;
using GapRemovalApp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace GapRemovalApp.UI
{
    public partial class VideoProcessingConfigForm : Window
    {
        private static readonly string ConfigPath = VideoProcessingConfig.GetConfigPath();

        private string presetSelecionado = "medium";
        private int crfSelecionado = 23;
        private int fpsSelecionado = 30;



        public VideoProcessingConfigForm()
        {
            InitializeComponent();

            QualityLevelComboBox.SelectionChanged += QualityLevelComboBox_SelectionChanged;
            FpsComboBox.SelectionChanged += FpsComboBox_SelectionChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HardwareDeviceComboBox.Items.Clear();

            var disponiveis = HardwareHelper.GetHardwareAccelerationOptions(); // Apenas encoders da sua máquina

            foreach (var kvp in disponiveis)
            {
                if (HardwareHelper.CodecDisplayMap.TryGetValue(kvp.Value, out var display))
                {
                    HardwareDeviceComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = display.DisplayName,
                        Tag = kvp.Value
                    });
                }
            }

            PreencherComboBoxDeThreads(ThreadsComboBox);

            var config = LoadSavedConfig();

            if (config != null)
            {
                var selected = HardwareDeviceComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == config.VideoEncoder);
                if (selected != null)
                    HardwareDeviceComboBox.SelectedItem = selected;
                else
                    HardwareDeviceComboBox.SelectedIndex = 0;

                QualityLevelComboBox.SelectedIndex = config.QualityLevelIndex >= 0 ? config.QualityLevelIndex : 2;
                FpsComboBox.SelectedIndex = config.TargetFps == 60 ? 1 : 0;

                var threadItem = ThreadsComboBox.Items.Cast<string>()
                    .FirstOrDefault(t => t == config.Threads.ToString());
                if (threadItem != null)
                    ThreadsComboBox.SelectedItem = threadItem;
            }
            else
            {
                HardwareDeviceComboBox.SelectedIndex = 0;
                QualityLevelComboBox.SelectedIndex = 2;
                FpsComboBox.SelectedIndex = 0;
                ThreadsComboBox.SelectedIndex = 0;
            }
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? selectedEncoder = (HardwareDeviceComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "libx264";
                int qualityIndex = QualityLevelComboBox.SelectedIndex;
                string[] niveis = { "muito alta", "alta", "média", "baixa" };
                string qualidade = niveis.ElementAtOrDefault(qualityIndex) ?? "média";

                var (preset, crf) = HardwareHelper.CodecDisplayMap[selectedEncoder].Item2[qualidade];

                var config = new VideoProcessingConfig
                {
                    Preset = preset,
                    Crf = crf,
                    VideoEncoder = selectedEncoder,
                    QualityLevelIndex = qualityIndex,
                    Threads = int.TryParse(ThreadsComboBox.SelectedItem?.ToString(), out var t) ? t : 0,
                    TargetFps = int.TryParse((FpsComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(), out var fps) ? fps : 30
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                MessageBox.Show("Configurações salvas com sucesso!");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar configurações: " + ex.Message);
            }
        }

        private VideoProcessingConfig? LoadSavedConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<VideoProcessingConfig>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar configuração: " + ex.Message);
            }
            return null;
        }

        private void PreencherComboBoxDeThreads(System.Windows.Controls.ComboBox comboBox)
        {
            int maxThreads = Environment.ProcessorCount;
            comboBox.Items.Clear();
            comboBox.Items.Add("0");

            for (int i = 1; i <= maxThreads; i++)
                comboBox.Items.Add(i.ToString());

            comboBox.SelectedIndex = 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void QualityLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = QualityLevelComboBox.SelectedItem as ComboBoxItem;
            string qualidade = item?.Content?.ToString()?.ToLower() ?? "média";

            var selectedItem = HardwareDeviceComboBox.SelectedItem as ComboBoxItem;
            string encoder = selectedItem?.Tag?.ToString() ?? "libx264";

            if (HardwareHelper.CodecDisplayMap.TryGetValue(encoder, out var encoderData)
                && encoderData.Item2.TryGetValue(qualidade, out var presetCrf))
            {
                presetSelecionado = presetCrf.Preset;
                crfSelecionado = presetCrf.Crf;
            }
            else
            {
                presetSelecionado = "medium";
                crfSelecionado = 23;
            }
        }

        private void FpsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = FpsComboBox.SelectedItem as ComboBoxItem;
            if (int.TryParse(item?.Content?.ToString(), out var selected))
                fpsSelecionado = selected;
        }
    }
}
