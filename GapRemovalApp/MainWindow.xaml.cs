using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using GapRemovalApp.Video;

namespace GapRemovalApp
{
    public partial class MainWindow : Window
    {
        private string videoPath = null!;
        private int silenceThreshold = -40;

        public MainWindow()
        {
            InitializeComponent();
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
                VideoLabel.Text = $"Vídeo: {videoPath}";
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessButton.Content = "Processando...";

            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                MessageBox.Show("Erro: O caminho do vídeo é inválido ou o arquivo não existe!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                ProcessButton.Content = "Abrir Preview";
                return;
            }

            try
            {
                var silentParts = await VideoProcessor.OnlyDetectSilence(videoPath, silenceThreshold);

                

                if (silentParts.Count == 0)
                {
                    MessageBox.Show("Nenhum trecho silencioso detectado. Ajuste a sensibilidade de silêncio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProcessButton.Content = "Abrir Preview";
                    return;
                }

                var previewWindow = new PreviewWindow(videoPath, silentParts);
                previewWindow.Show();

                ProcessButton.Content = "Abrir Preview";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar o vídeo:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                ProcessButton.Content = "Abrir Preview";
            }
        }
    }
}