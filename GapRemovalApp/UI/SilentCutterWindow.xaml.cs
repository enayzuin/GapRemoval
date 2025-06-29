using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GapRemovalApp.Logging;
using GapRemovalApp.Utils;
using GapRemovalApp.Video;
using Serilog;
using MessageBox = System.Windows.MessageBox;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Linq;

namespace GapRemovalApp.UI
{
    public partial class SilentCutterWindow : Window
    {
        private string videoPath = null!;
        private string wavPath = null!;
        private int silenceThreshold = -40;
        private int minDelay = 700;

        // Guardar os silêncios detectados para uso no timer
        private List<(TimeSpan start, TimeSpan end)> _ultimosSilencios = new();

        private VideoTimeCoordinator _timeCoordinator = new VideoTimeCoordinator();
        private DispatcherTimer? _syncTimer;

        public SilentCutterWindow()
        {
            try
            {
                InitializeComponent();
                
                // Garantir que a janela sempre inicie maximizada
                WindowState = WindowState.Maximized;
                
                // Configurar para sempre maximizar ao abrir
                StateChanged += (s, e) =>
                {
                    if (WindowState == WindowState.Normal)
                    {
                        WindowState = WindowState.Maximized;
                    }
                };
                
                SensitivityControl.SensitivityChanged += OnSensitivityChanged;
                DelaySensitivityControl.DelayChanged += OnChangeMinSilenceMs;

                Logger.Setup();
                Log.Information("SilentCutterWindow inicializada.");

                // Sincronizar UI com o tempo centralizado
                _timeCoordinator.TimeChanged += OnTimeChanged;
                _timeCoordinator.DurationChanged += d => WaveformControl.SetDuration(d);
                
                // Ajustar tamanho dos controles quando a janela for redimensionada
                SizeChanged += OnWindowSizeChanged;
                
                // Ajustar controles quando a janela for carregada
                Loaded += OnWindowLoaded;
                
                // Inicializar variáveis locais com os valores dos controles
                silenceThreshold = (int)SensitivityControl.CurrentValue;
                minDelay = DelaySensitivityControl.CurrentDelay;
                Log.Information("Valores iniciais - Sensibilidade: {Sensitivity} dB, Delay: {Delay} ms", silenceThreshold, minDelay);
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro no construtor da SilentCutterWindow: {ex}");
                MessageBox.Show("Erro crítico ao abrir o editor de vídeo. Veja o log para detalhes.");
            }
        }
        
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Ajustar controles na primeira vez que a janela for carregada
            OnWindowSizeChanged(this, null);
        }
        
        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Ajustar o tamanho do container de vídeo baseado no tamanho da janela
            var availableHeight = ActualHeight - 200; // Reservar espaço para header, controles e botões
            var availableWidth = ActualWidth - 100;   // Reservar espaço para margens
            
            if (VideoContainer != null)
            {
                VideoContainer.Height = Math.Max(400, availableHeight * 0.7); // 70% da altura disponível
                VideoContainer.Width = Math.Max(800, availableWidth * 0.8);   // 80% da largura disponível
            }
            
            if (WaveformControl != null)
            {
                WaveformControl.Height = Math.Max(150, availableHeight * 0.2); // 20% da altura disponível
                WaveformControl.Width = Math.Max(800, availableWidth * 0.8);   // 80% da largura disponível (restaurado)
            }
            
            // Garantir que o overlay cubra todo o container de vídeo
            if (VideoOverlayGrid != null && VideoContainer != null)
            {
                VideoOverlayGrid.Width = VideoContainer.Width;
                VideoOverlayGrid.Height = VideoContainer.Height;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Vídeos (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv|Todos os Arquivos|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    videoPath = openFileDialog.FileName;
                    Log.Information("Vídeo selecionado: {VideoPath}", videoPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro ao selecionar vídeo: {ex}");
                MessageBox.Show("Erro ao selecionar vídeo. Veja o log para detalhes.");
            }
        }

        private async void OpenVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Vídeos (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv|Todos os Arquivos|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    videoPath = openFileDialog.FileName;
                    Log.Information("Abrindo vídeo: {VideoPath}", videoPath);

                    try
                    {
                        // Mostrar painel de vídeo
                        NoVideoMessage.Visibility = Visibility.Collapsed;
                        VideoPanel.Visibility = Visibility.Visible;
                        VlcPlayer.Visibility = Visibility.Visible;

                        // Inicializar overlay
                        VideoOverlayGrid.Visibility = Visibility.Collapsed;
                        if (VideoContainer != null)
                        {
                            VideoOverlayGrid.Width = VideoContainer.Width;
                            VideoOverlayGrid.Height = VideoContainer.Height;
                        }

                        // Carregar vídeo no player
                        VlcPlayer.LoadVideo(videoPath);

                        // Aguardar um pouco para o VLC carregar o vídeo
                        await Task.Delay(1000);

                        // Extrair áudio
                        wavPath = await Audio.ExtrairWav(videoPath);
                        Log.Information("WAV extraído: {WavPath}", wavPath);

                        // Renderizar waveform com dados reais do áudio
                        WaveformControl.RenderWaveform(wavPath);
                        
                        // Obter duração do vídeo e do WAV para comparação
                        var videoDuration = VlcPlayer.GetDuration();
                        var wavDuration = await GetDurationFromWav(wavPath);
                        Log.Information($"Duração do vídeo: {videoDuration.TotalSeconds:F2}s | Duração do WAV: {wavDuration.TotalSeconds:F2}s");

                        // Sempre usar a duração do vídeo para o SetDuration, só usar do WAV se for zero
                        if (videoDuration.TotalSeconds > 0)
                        {
                            WaveformControl.SetDuration(videoDuration);
                            _timeCoordinator.SetDuration(videoDuration);
                        }
                        else
                        {
                            WaveformControl.SetDuration(wavDuration);
                            _timeCoordinator.SetDuration(wavDuration);
                        }

                        // Detectar silêncios usando os valores atuais dos controles
                        var currentSensitivity = SensitivityControl.CurrentValue;
                        var currentDelay = DelaySensitivityControl.CurrentDelay;
                        var silentParts = Audio.DetectarSilencios(wavPath, (int)currentSensitivity, currentDelay);
                        Log.Information("Silêncios detectados: {Count} (Sensibilidade: {Sensitivity} dB, Delay: {Delay} ms)", 
                                       silentParts.Count, currentSensitivity, currentDelay);
                        
                        // Log detalhado dos silêncios detectados
                        foreach (var (start, end) in silentParts)
                        {
                            Log.Debug($"[SILENCE] {start.TotalMilliseconds}ms - {end.TotalMilliseconds}ms (duração: {(end-start).TotalMilliseconds}ms)");
                        }
                        
                        WaveformControl.DestacarSilencios(silentParts);
                        _ultimosSilencios = silentParts;

                        // Parar timer anterior se existir
                        _syncTimer?.Stop();

                        // Timer centralizado
                        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                        _syncTimer.Tick += (_, _) =>
                        {
                            try
                            {
                                var pos = VlcPlayer.GetPosition();
                                Log.Debug($"[TIMER] Tick - pos: {pos.TotalMilliseconds} ms");
                                _timeCoordinator.SetTime(pos);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Erro ao atualizar tempo centralizado");
                            }
                        };
                        _syncTimer.Start();

                        // Quando clicar no waveform, faz seek e atualiza tempo central
                        WaveformControl.TimeSelected += (time) =>
                        {
                            try
                            {
                                VlcPlayer.Seek(time);
                                _timeCoordinator.SetTime(time);
                                Log.Information("Seek para: {Time}", time);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Erro ao fazer seek no vídeo");
                            }
                        };

                        System.Windows.MessageBox.Show("Vídeo carregado com sucesso!", "Sucesso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Erro ao carregar vídeo");
                        MessageBox.Show($"Erro ao carregar vídeo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro ao abrir vídeo: {ex}");
                MessageBox.Show("Erro ao abrir vídeo. Veja o log para detalhes.");
            }
        }

        private async Task<TimeSpan> GetDurationFromWav(string wavPath)
        {
            try
            {
                using var reader = new AudioFileReader(wavPath);
                var duration = reader.TotalTime;
                return duration;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao obter duração do WAV");
                return TimeSpan.FromSeconds(60); // Duração padrão de 1 minuto
            }
        }

        private void OnSensitivityChanged(float newThreshold)
        {
            silenceThreshold = (int)newThreshold;
            Log.Information("Sensibilidade alterada para: {Threshold}", silenceThreshold);

            if (!string.IsNullOrEmpty(wavPath))
            {
                var currentSensitivity = SensitivityControl.CurrentValue;
                var currentDelay = DelaySensitivityControl.CurrentDelay;
                var silentParts = Audio.DetectarSilencios(wavPath, (int)currentSensitivity, currentDelay);
                WaveformControl.DestacarSilencios(silentParts);
                _ultimosSilencios = silentParts;
                Log.Information("Silêncios atualizados: {Count} (Sensibilidade: {Sensitivity} dB, Delay: {Delay} ms)", 
                               silentParts.Count, currentSensitivity, currentDelay);
            }
        }

        private void OnChangeMinSilenceMs(int newDelay)
        {
            minDelay = newDelay;
            Log.Information("Delay mínimo alterado para: {MinDelay} ms", minDelay);

            if (!string.IsNullOrEmpty(wavPath))
            {
                var currentSensitivity = SensitivityControl.CurrentValue;
                var currentDelay = DelaySensitivityControl.CurrentDelay;
                var silentParts = Audio.DetectarSilencios(wavPath, (int)currentSensitivity, currentDelay);
                WaveformControl.DestacarSilencios(silentParts);
                _ultimosSilencios = silentParts;
                Log.Information("Silêncios atualizados: {Count} (Sensibilidade: {Sensitivity} dB, Delay: {Delay} ms)", 
                               silentParts.Count, currentSensitivity, currentDelay);
            }
        }

        private void OnTimeChanged(TimeSpan time)
        {
            Log.Debug($"[UI] OnTimeChanged: {time.TotalMilliseconds}ms");
            // Atualiza agulha do waveform
            WaveformControl.UpdateCursor(time);
            // Overlay vermelho sincronizado - usar a mesma lista de silêncios do waveform
            bool emSilencio = _ultimosSilencios.Any(s => time >= s.start && time <= s.end);
            
            // Log para debug do overlay
            if (emSilencio)
            {
                Log.Debug($"[OVERLAY] Mostrando overlay vermelho em {time.TotalMilliseconds}ms");
            }
            
            VideoOverlayGrid.Visibility = emSilencio ? Visibility.Visible : Visibility.Collapsed;
            
            // Garantir que o overlay cubra todo o container
            if (emSilencio && VideoContainer != null)
            {
                VideoOverlayGrid.Width = VideoContainer.Width;
                VideoOverlayGrid.Height = VideoContainer.Height;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VlcPlayer.Pause();

                if (string.IsNullOrEmpty(wavPath) || string.IsNullOrEmpty(videoPath))
                {
                    MessageBox.Show("Por favor, selecione um vídeo primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadingOverlay.Visibility = Visibility.Visible;
                VideoPanel.IsEnabled = false;
                SensitivityControl.IsEnabled = false;
                DelaySensitivityControl.IsEnabled = false;
                WaveformControl.IsEnabled = false;
                NoVideoMessage.IsEnabled = false;

                ExportButton.Visibility = Visibility.Collapsed;
                OpenButton.Visibility = Visibility.Collapsed;
                VideoPanel.Visibility = Visibility.Collapsed;
                SensitivityControl.Visibility = Visibility.Collapsed;
                DelaySensitivityControl.Visibility = Visibility.Collapsed;

                var outputFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Vídeos MP4 (*.mp4)|*.mp4|Todos os Arquivos (*.*)|*.*",
                    FileName = "output_video"
                };

                if (outputFileDialog.ShowDialog() == true)
                {
                    ExportProgressBar.Value = 0;
                    ExportStatusText.Text = "Iniciando exportação...";
                    Log.Information("Exportando vídeo para: {Output}", outputFileDialog.FileName);

                    var currentSensitivity = SensitivityControl.CurrentValue;
                    var currentDelay = DelaySensitivityControl.CurrentDelay;
                    var silentParts = Audio.DetectarSilencios(wavPath, (int)currentSensitivity, currentDelay);
                    Log.Information("Exportando com sensibilidade: {Sensitivity} dB, Delay: {Delay} ms", currentSensitivity, currentDelay);

                    var exportFiles = await VideoProcessor.CutVideo(videoPath, silentParts, progress =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ExportProgressBar.Value = progress;
                            ExportStatusText.Text = $"Exportando... {progress:0}%";
                        });
                    });

                    await VideoProcessor.ConcatenateVideos(outputFileDialog.FileName, exportFiles);

                    if (exportFiles.Count > 0)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(outputFileDialog.FileName));
                        MessageBox.Show($"Vídeo exportado com sucesso! {exportFiles.Count} partes criadas.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        Log.Information("Exportação concluída. {Count} partes criadas.", exportFiles.Count);
                    }
                    else
                    {
                        MessageBox.Show("Nenhuma parte foi exportada. Verifique os silêncios detectados.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Log.Warning("Exportação falhou. Nenhuma parte criada.");
                    }
                }
                Log.Debug("Exportação de vídeo iniciada.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro ao exportar vídeo: {ex}");
                MessageBox.Show("Erro ao exportar vídeo. Veja o log para detalhes.");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                VideoPanel.IsEnabled = true;
                SensitivityControl.IsEnabled = true;
                DelaySensitivityControl.IsEnabled = true;
                WaveformControl.IsEnabled = true;

                ExportButton.Visibility = Visibility.Visible;
                OpenButton.Visibility = Visibility.Visible;
                VideoPanel.Visibility = Visibility.Visible;
                SensitivityControl.Visibility = Visibility.Visible;
                DelaySensitivityControl.Visibility = Visibility.Visible;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _syncTimer?.Stop();
            Log.Information("Aplicação encerrada manualmente.");
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void TestWaveform_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mostrar painel de vídeo
                NoVideoMessage.Visibility = Visibility.Collapsed;
                VideoPanel.Visibility = Visibility.Visible;
                WaveformControl.Visibility = Visibility.Visible;

                System.Windows.MessageBox.Show("Testando renderização do waveform...", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                // Testar waveform
                WaveformControl.RenderTestWaveform();

                // Configurar duração de teste
                WaveformControl.SetDuration(TimeSpan.FromSeconds(60)); // 1 minuto de teste
                
                // Testar overlay com silêncios simulados
                var testSilences = new List<(TimeSpan start, TimeSpan end)>
                {
                    (TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)),
                    (TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35)),
                    (TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(55))
                };
                
                WaveformControl.DestacarSilencios(testSilences);
                _ultimosSilencios = testSilences;
                
                // Inicializar overlay para teste
                if (VideoContainer != null)
                {
                    VideoOverlayGrid.Width = VideoContainer.Width;
                    VideoOverlayGrid.Height = VideoContainer.Height;
                }

                System.Windows.MessageBox.Show("Teste do waveform concluído! Overlay configurado para testar silêncios em 10-15s, 30-35s e 50-55s.", "Sucesso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao testar waveform");
                MessageBox.Show($"Erro ao testar waveform: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
