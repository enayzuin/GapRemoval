using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GapRemovalApp.Video;

namespace GapRemovalApp
{
    public partial class PreviewWindow : Window
    {
        private string videoPath = null!;
        private List<(TimeSpan start, TimeSpan end)> silentParts;
        private int silenceThreshold = -40;
        private DispatcherTimer sliderTimer = null!;

        public PreviewWindow(string videoPath, List<(TimeSpan start, TimeSpan end)> silentParts)
        {
            InitializeComponent();

            this.videoPath = videoPath;
            this.silentParts = silentParts;

            if (silentParts == null || silentParts.Count == 0)
            {
                MessageBox.Show("Nenhum trecho silencioso detectado. Ajuste a sensibilidade de silêncio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            VideoLabel.Text = $"Vídeo: {videoPath}";

            sliderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            sliderTimer.Tick += SliderTimer_Tick;

            SensitivitySlider.Value = silenceThreshold;
            SensitivityValueText.Text = silenceThreshold.ToString("F2", CultureInfo.InvariantCulture);

            LoadSilentParts();
        }

        private void LoadSilentParts()
        {
            SilentPartsList.Items.Clear();

            foreach (var (start, end) in silentParts)
            {
                string formattedStart = start.ToString(@"hh\:mm\:ss");
                string formattedEnd = end.ToString(@"hh\:mm\:ss");
                SilentPartsList.Items.Add($"{formattedStart} - {formattedEnd}");
            }
        }

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SensitivityValueText != null)
            {
                silenceThreshold = (int)e.NewValue;
                SensitivityValueText.Text = silenceThreshold.ToString("F2", CultureInfo.InvariantCulture);
            }
            sliderTimer?.Stop();
            sliderTimer?.Start();

        }

        private async void SliderTimer_Tick(object? sender, EventArgs e)
        {
            sliderTimer.Stop();
            ProgressBar.Visibility = Visibility.Visible;

            try
            {
                silentParts = VideoProcessor.OnlyDetectSilence(videoPath, silenceThreshold);
                LoadSilentParts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao recalcular os silêncios:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessButton.IsEnabled = false;
            ProcessButton.Content = "Processando...";

            try
            {
                silentParts = VideoProcessor.OnlyDetectSilence(videoPath, silenceThreshold);
                var parts = await VideoProcessor.CutVideo(videoPath, silentParts);
                await VideoProcessor.ConcatenateVideos(videoPath, parts);

                MessageBox.Show("✅ Vídeo processado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar vídeo:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                ProcessButton.IsEnabled = true;
                ProcessButton.Content = "Exportar Vídeo";
            }
        }
    }
}
