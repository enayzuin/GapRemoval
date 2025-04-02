using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using GapRemovalApp.Utils;
using GapRemovalApp.Video;

namespace GapRemovalApp
{
    public partial class MainWindow : Window
    {
        private string videoPath = null!;
        private string wavPath = null!;
        private int silenceThreshold = -40;

        public MainWindow()
        {
            InitializeComponent();
            SensitivityControl.SensitivityChanged += OnSensitivityChanged;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Vídeos (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv|Todos os Arquivos|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                videoPath = openFileDialog.FileName;

            }
        }


        private async void OpenVideo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Vídeos (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv|Todos os Arquivos|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {   
                string selectedVideo = openFileDialog.FileName;
                videoPath = selectedVideo;
                // Exibe o painel com vídeo e waveform
                VideoPanel.Visibility = Visibility.Visible;

                // Carrega e toca o vídeo
                VideoPlayerControl.Visibility = Visibility.Visible;
                VideoPlayerControl.LoadVideo(selectedVideo);

                // Converte vídeo em WAV (para gerar o waveform)
                wavPath = await Audio.ExtrairWav(selectedVideo);

                // Renderiza o waveform
                WaveformControl.RenderWaveform(wavPath);

                // Seta duração para o controle saber o tamanho do cursor
                WaveformControl.SetDuration(VideoPlayerControl.GetDuration());

                var silentParts = Audio.DetectarSilencios(wavPath);
                WaveformControl.DestacarSilencios(silentParts);

                // Sincroniza cursor enquanto o vídeo toca
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                timer.Tick += (_, _) =>
                {
                    var pos = VideoPlayerControl.GetPosition();
                    WaveformControl.UpdateCursor(pos);
                };
                timer.Start();

                // Evento ao clicar no waveform → atualiza o vídeo
                WaveformControl.TimeSelected += (time) =>
                {
                    VideoPlayerControl.Seek(time);
                };

     
            }


        }

        private void OnSensitivityChanged(float newThreshold)
        {
            silenceThreshold = (int) newThreshold;
            if (!string.IsNullOrEmpty(wavPath))
            {
                var silentParts = Audio.DetectarSilencios(wavPath, newThreshold);
                WaveformControl.DestacarSilencios(silentParts);
            }
        }


        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(wavPath) || string.IsNullOrEmpty(videoPath))
            {
                MessageBox.Show("Por favor, selecione um vídeo primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Exibe o overlay de carregamento
            LoadingOverlay.Visibility = Visibility.Visible;

            // Desabilita os controles da interface
            VideoPanel.IsEnabled = false;
            SensitivityControl.IsEnabled = false;
            VideoPlayerControl.IsEnabled = false;
            WaveformControl.IsEnabled = false;


            ExportButton.Visibility = Visibility.Collapsed;
            OpenButton.Visibility = Visibility.Collapsed;
            VideoPanel.Visibility = Visibility.Collapsed;
            SensitivityControl.Visibility = Visibility.Collapsed;
            VideoPlayerControl.Visibility = Visibility.Collapsed;

            try
            {
                var outputFilePath = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Vídeos MP4 (*.mp4)|*.mp4|Todos os Arquivos (*.*)|*.*",
                    FileName = "output_video"  // Nome padrão do arquivo
                };
                if (outputFilePath.ShowDialog() == true)
                {
                    // Detecção de silêncios baseada na sensibilidade ajustada
                    var silentParts = Audio.DetectarSilencios(wavPath, silenceThreshold);

                    // Chamando o método CutVideo para realizar a exportação do vídeo
                    var exportFiles = await VideoProcessor.CutVideo(videoPath, silentParts);
                    await VideoProcessor.ConcatenateVideos(outputFilePath.FileName, exportFiles);

                    // Notificando o usuário que o processo foi concluído
                    if (exportFiles.Count > 0)
                    {
                        MessageBox.Show($"Vídeo exportado com sucesso! {exportFiles.Count} partes criadas.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Nenhuma parte foi exportada. Verifique os silêncios detectados.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar vídeo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Esconde o overlay de carregamento e reabilita os controles
                LoadingOverlay.Visibility = Visibility.Collapsed;
                VideoPanel.IsEnabled = true;
                SensitivityControl.IsEnabled = true;
                VideoPlayerControl.IsEnabled = true;
                WaveformControl.IsEnabled = true;

                ExportButton.Visibility = Visibility.Visible;
                OpenButton.Visibility = Visibility.Visible;
                VideoPanel.Visibility = Visibility.Visible;
                SensitivityControl.Visibility = Visibility.Visible;
                VideoPlayerControl.Visibility = Visibility.Visible;
            }
        }

    }
}