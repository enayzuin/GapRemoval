using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using GapRemovalApp.Utils;
using NAudio.Wave;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;

namespace GapRemovalApp
{
    public partial class PreviewWindow : Window
    {
        private readonly string videoPath;
        private readonly string previewVideoPath;
        private readonly string wavPath;
        private DispatcherTimer? timer;

        public PreviewWindow(string path, string previewPath, string wavPath, List<(TimeSpan start, TimeSpan end)> silentParts)
        {
            InitializeComponent();
            videoPath = path;
            previewVideoPath = previewPath;
            this.wavPath = wavPath;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Garante que o MediaElement esteja carregado visualmente antes de atribuir o Source
            Dispatcher.BeginInvoke(async () =>
            {
                Player.MediaOpened += (_, _) =>
                {
                    var duration = Player.NaturalDuration.TimeSpan;

                    ProgressSlider.Minimum = 0;
                    ProgressSlider.Maximum = duration.TotalSeconds;
                    TotalTimeText.Text = FormatTime(duration);

                    timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(200)
                    };
                    timer.Tick += Timer_Tick;
                    timer.Start();

                    RenderWaveform(wavPath);
                    Player.Play();
                };

                Player.MediaFailed += (_, err) =>
                {
                    MessageBox.Show("❌ MediaFailed: " + err.ErrorException?.Message);
                };

                Player.Source = new Uri(previewVideoPath);
            }, DispatcherPriority.Loaded);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                ProgressSlider.Value = Player.Position.TotalSeconds;
                CurrentTimeText.Text = FormatTime(Player.Position);
            }
        }

        private string FormatTime(TimeSpan time)
        {
            return time.Hours > 0 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
        }

        private void RenderWaveform(string wavPath)
        {
            WaveformCanvas.Children.Clear();

            using var reader = new AudioFileReader(wavPath);
            int resolution = 500;
            int sampleCount = (int)(reader.Length / sizeof(float));
            float[] buffer = new float[sampleCount];
            reader.Read(buffer, 0, buffer.Length);

            WaveformCanvas.UpdateLayout();

            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;

            if (width == 0 || height == 0)
            {
                WaveformCanvas.LayoutUpdated += (_, _) => RenderWaveform(wavPath);
                return;
            }

            int step = Math.Max(1, buffer.Length / resolution);
            double barWidth = width / resolution;

            for (int i = 0; i < resolution; i++)
            {
                int index = i * step;
                if (index >= buffer.Length) break;

                float amplitude = Math.Abs(buffer[index]);
                double barHeight = amplitude * height * 2;

                var rect = new Rectangle
                {
                    Width = barWidth - 1,
                    Height = barHeight,
                    Fill = Brushes.LightGreen
                };

                Canvas.SetLeft(rect, i * barWidth);
                Canvas.SetTop(rect, (height - barHeight) / 2);
                WaveformCanvas.Children.Add(rect);
            }
        }
    }
}
