using System;
using System.Windows.Controls;
using NAudio.Wave;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System;
using System.Windows;


namespace GapRemovalApp
{
    public partial class WaveformControl : UserControl
    {
        private double videoDurationSeconds = 0;
        public event Action<TimeSpan>? TimeSelected;
         public event Action? TogglePlayPause;

        public WaveformControl()
        {
            InitializeComponent();
        }

        public void SetDuration(TimeSpan duration)
        {
            videoDurationSeconds = duration.TotalSeconds;
            TimeCursor.Visibility = Visibility.Visible;
        }
        

        public void UpdateCursor(TimeSpan position)
        {
            if (videoDurationSeconds <= 0) return;

            double percent = position.TotalSeconds / videoDurationSeconds;
            double x = percent * WaveformCanvas.ActualWidth;

            Canvas.SetLeft(TimeCursor, x);
            TimeCursor.X1 = 0;
            TimeCursor.X2 = 0;
            TimeCursor.Y1 = 0;
            TimeCursor.Y2 = WaveformCanvas.ActualHeight;
        }

        private void WaveformCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (videoDurationSeconds <= 0) return;

            Point clickPosition = e.GetPosition(WaveformCanvas);
            double percent = clickPosition.X / WaveformCanvas.ActualWidth;
            double seconds = percent * videoDurationSeconds;

            UpdateCursor(TimeSpan.FromSeconds(seconds));
            TimeSelected?.Invoke(TimeSpan.FromSeconds(seconds));
        }

        public void RenderWaveform(string wavPath)
        {
            WaveformCanvas.Children.Clear();
            WaveformCanvas.Children.Add(TimeCursor); // Re-adiciona o cursor

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
                    Width = barWidth + 1,
                    Height = barHeight * 1.2,
                    Fill = Brushes.LightGreen
                };

                Canvas.SetLeft(rect, i * barWidth);
                Canvas.SetTop(rect, (height - barHeight) / 2);
                WaveformCanvas.Children.Add(rect);
            }
        }
        public void DestacarSilencios(List<(TimeSpan start, TimeSpan end)> silencios)
        {
            if (videoDurationSeconds <= 0) return;

            // 🔥 Remove todos os retângulos vermelhos anteriores (Tag="Silence")
            for (int i = WaveformCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (WaveformCanvas.Children[i] is Rectangle rect && rect.Tag?.ToString() == "Silence")
                {
                    WaveformCanvas.Children.RemoveAt(i);
                }
            }

            // 🔁 Desenha os novos trechos silenciosos
            foreach (var (start, end) in silencios)
            {
                double startX = (start.TotalSeconds / videoDurationSeconds) * WaveformCanvas.ActualWidth;
                double endX = (end.TotalSeconds / videoDurationSeconds) * WaveformCanvas.ActualWidth;
                double width = Math.Max(1, endX - startX);

                var rect = new Rectangle
                {
                    Width = width,
                    Height = WaveformCanvas.ActualHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(204, 255, 0, 0)), // vermelho com 80% de transparência
                    IsHitTestVisible = false,
                    Tag = "Silence" // 👈 facilita a limpeza
                };

                Canvas.SetLeft(rect, startX);
                Canvas.SetTop(rect, 0);
                WaveformCanvas.Children.Add(rect);
            }
        }


    }
}
