using System;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows;

namespace GapRemovalApp.Player
{
    public class VideoPlayerWithWaveform
    {
        private MediaElement mediaElement;
        private Slider progressSlider;
        private Canvas waveformCanvas;
        private TextBlock currentTimeText;
        private TextBlock totalTimeText;
        private DispatcherTimer timer;
        private bool isSeeking = false;

        public VideoPlayerWithWaveform(MediaElement mediaElement,
                                       Slider slider,
                                       Canvas waveformCanvas,
                                       TextBlock currentTimeText,
                                       TextBlock totalTimeText)
        {
            this.mediaElement = mediaElement;
            this.progressSlider = slider;
            this.waveformCanvas = waveformCanvas;
            this.currentTimeText = currentTimeText;
            this.totalTimeText = totalTimeText;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            timer.Tick += Timer_Tick;

            mediaElement.LoadedBehavior = MediaState.Manual;
            mediaElement.UnloadedBehavior = MediaState.Manual;
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.MediaFailed += MediaElement_MediaFailed;

            slider.ValueChanged += Slider_ValueChanged;
            slider.PreviewMouseDown += (_, _) => isSeeking = true;
            slider.PreviewMouseUp += (_, _) =>
            {
                isSeeking = false;
                mediaElement.Position = TimeSpan.FromSeconds(slider.Value);
            };
        }

        private void MediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("❌ Erro ao carregar o vídeo: " + e.ErrorException.Message);
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("✅ Evento MediaOpened disparado!");

            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                var duration = mediaElement.NaturalDuration.TimeSpan;
                progressSlider.Minimum = 0;
                progressSlider.Maximum = duration.TotalSeconds;
                totalTimeText.Text = FormatTime(duration);

                timer.Start();
                mediaElement.Play();
            }
            else
            {
                System.Windows.MessageBox.Show("⚠️ O vídeo foi carregado, mas não tem duração conhecida.");
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isSeeking && mediaElement.NaturalDuration.HasTimeSpan)
            {
                progressSlider.Value = mediaElement.Position.TotalSeconds;
                currentTimeText.Text = FormatTime(mediaElement.Position);
                UpdateWaveformIndicator(mediaElement.Position.TotalSeconds);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isSeeking)
            {
                UpdateWaveformIndicator(e.NewValue);
                currentTimeText.Text = FormatTime(TimeSpan.FromSeconds(e.NewValue));
            }
        }

        private void UpdateWaveformIndicator(double seconds)
        {
            waveformCanvas.Children.Clear();
            double x = seconds / progressSlider.Maximum * waveformCanvas.ActualWidth;

            var line = new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = waveformCanvas.ActualHeight,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2
            };

            waveformCanvas.Children.Add(line);
        }

        private string FormatTime(TimeSpan time)
        {
            return time.Hours > 0 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
        }

        public void LoadVideo(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                System.Windows.MessageBox.Show("❌ Arquivo não encontrado: " + path);
                return;
            }

            try
            {
                mediaElement.Source = new Uri(path);
                mediaElement.Position = TimeSpan.Zero;
                System.Windows.MessageBox.Show("🎬 Source definida: " + path);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("❌ Falha ao definir Source do vídeo: " + ex.Message);
            }
        }

        public void Play() => mediaElement.Play();
        public void Pause() => mediaElement.Pause();
        public void Stop()
        {
            mediaElement.Stop();
            timer.Stop();
        }

        public void Dispose()
        {
            Stop();
            mediaElement.Source = null;
        }
    }
}