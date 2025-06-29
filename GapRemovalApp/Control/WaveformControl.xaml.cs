using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Wave;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace GapRemovalApp
{
    public partial class WaveformControl : System.Windows.Controls.UserControl
    {
        private double videoDurationSeconds = 0;
        private string wavPath = string.Empty; // Este campo não está sendo usado diretamente, mas mantido.
        public event Action<TimeSpan>? TimeSelected;
        public event Action? TogglePlayPause; // Este evento não está sendo usado diretamente, mas mantido.

        private List<(TimeSpan start, TimeSpan end)> _silencios = new();
        private string _lastWavPath = string.Empty;

        // Campo para armazenar a duração real do áudio carregado
        private double _currentAudioDurationSeconds = 0; 

        // O TimeCursor é um elemento x:Name no XAML, então não precisa ser declarado aqui.
        // A linha 'public Line TimeCursor { get; set; }' foi removida conforme a discussão anterior.

        public WaveformControl()
        {
            InitializeComponent();
            // Se TimeCursor não fosse um elemento x:Name no XAML, seria inicializado aqui.
            // Como ele está no XAML, o compilador do WPF já o associa automaticamente.
        }

        public void SetDuration(TimeSpan duration)
        {
            videoDurationSeconds = duration.TotalSeconds;
            // A visibilidade do cursor agora será controlada principalmente por UpdateCursor
        }
        

        public void UpdateCursor(TimeSpan position)
        {
            try
            {
                // GARANTIR THREAD DA UI: Se não estiver na thread da UI, invocar nela.
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => UpdateCursor(position));
                    return;
                }

                // USAR DURAÇÃO CENTRALIZADA: Priorizar a duração do áudio carregado.
                double effectiveDurationSeconds = _currentAudioDurationSeconds;

                // Fallback para a duração do vídeo se o áudio ainda não foi carregado ou falhou.
                // Idealmente, RenderWaveform deve ser chamado antes de UpdateCursor.
                if (effectiveDurationSeconds <= 0)
                {
                    effectiveDurationSeconds = videoDurationSeconds;
                    if (effectiveDurationSeconds <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WAVEFORM] UpdateCursor: duração efetiva zero, ignorando posição {position}");
                        // TimeCursor é um elemento x:Name, então ele existe.
                        TimeCursor.Visibility = Visibility.Collapsed; // Esconder cursor se não houver duração válida
                        return;
                    }
                }

                double canvasWidth = WaveformCanvas.ActualWidth;
                if (canvasWidth <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[WAVEFORM] UpdateCursor: largura do canvas zero");
                    // TimeCursor é um elemento x:Name, então ele existe.
                    TimeCursor.Visibility = Visibility.Collapsed; // Esconder cursor se o canvas não tiver largura
                    return;
                }

                double pixelsPerSecond = canvasWidth / effectiveDurationSeconds;
                double x = position.TotalSeconds * pixelsPerSecond;

                // Limitar a posição da agulha para não ultrapassar o limite do waveform
                x = Math.Max(0, Math.Min(canvasWidth, x));

                System.Diagnostics.Debug.WriteLine($"[WAVEFORM] UpdateCursor: pos={position.TotalMilliseconds}ms, effectiveDur={effectiveDurationSeconds}s, canvasWidth={canvasWidth}, pixelsPerSecond={pixelsPerSecond:F1}, x={x:F1}");

                // TimeCursor é um elemento x:Name, então ele existe.
                TimeCursor.X1 = x;
                TimeCursor.X2 = x;
                TimeCursor.Y1 = 0;
                TimeCursor.Y2 = WaveformCanvas.ActualHeight;
                TimeCursor.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao atualizar cursor: {ex.Message}");
            }
        }

        private void WaveformCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // USAR DURAÇÃO CENTRALIZADA: Priorizar a duração do áudio carregado.
            double effectiveDurationSeconds = _currentAudioDurationSeconds;

            if (effectiveDurationSeconds <= 0)
            {
                effectiveDurationSeconds = videoDurationSeconds;
                if (effectiveDurationSeconds <= 0) return; // Não fazer nada se não houver duração válida
            }

            System.Windows.Point clickPosition = e.GetPosition(WaveformCanvas);
            double canvasWidth = WaveformCanvas.ActualWidth;
            
            if (canvasWidth <= 0) return;

            double pixelsPerSecond = canvasWidth / effectiveDurationSeconds;
            
            // Limitar o clique para não ultrapassar o limite do waveform
            double limitedX = Math.Max(0, Math.Min(canvasWidth, clickPosition.X));
            double seconds = limitedX / pixelsPerSecond;

            System.Diagnostics.Debug.WriteLine($"[WAVEFORM] Click: x={clickPosition.X}, limitedX={limitedX}, canvasWidth={canvasWidth}, effectiveDur={effectiveDurationSeconds}s, pixelsPerSecond={pixelsPerSecond:F1}, seconds={seconds:F2}");

            UpdateCursor(TimeSpan.FromSeconds(seconds));
            TimeSelected?.Invoke(TimeSpan.FromSeconds(seconds));
        }

        public void RenderWaveform(string wavPath)
        {
            _lastWavPath = wavPath;
            RenderWaveformInternal(wavPath);
        }

        private void RenderWaveformInternal(string wavPath)
        {
            try
            {
                if (!File.Exists(wavPath))
                {
                    System.Windows.MessageBox.Show($"Arquivo WAV não encontrado: {wavPath}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    _currentAudioDurationSeconds = 0; // Resetar duração se o arquivo não for encontrado
                    return;
                }

                WaveformCanvas.Children.Clear();
                // TimeCursor é um elemento x:Name, então ele existe.
                WaveformCanvas.Children.Add(TimeCursor); // Re-adiciona o cursor para garantir que ele esteja sempre no topo

                // Forçar layout para obter dimensões corretas antes de desenhar
                WaveformCanvas.UpdateLayout();
                WaveformCanvas.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                WaveformCanvas.Arrange(new System.Windows.Rect(0, 0, WaveformCanvas.ActualWidth, WaveformCanvas.ActualHeight));

                double width = WaveformCanvas.ActualWidth;
                double height = WaveformCanvas.ActualHeight;

                if (width <= 0 || height <= 0)
                {
                    // Se as dimensões ainda não estiverem prontas, aguardar o evento LayoutUpdated
                    WaveformCanvas.LayoutUpdated -= OnLayoutUpdated; // Evitar múltiplas assinaturas
                    WaveformCanvas.LayoutUpdated += OnLayoutUpdated;
                    return;
                }

                using var reader = new AudioFileReader(wavPath);
                if (reader.Length <= 0)
                {
                    System.Windows.MessageBox.Show("Arquivo WAV não contém dados de áudio válidos.", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    _currentAudioDurationSeconds = 0; // Resetar duração se o arquivo for inválido
                    return;
                }

                _currentAudioDurationSeconds = reader.TotalTime.TotalSeconds;
                System.Diagnostics.Debug.WriteLine($"[WAVEFORM] Audio Duration Set: {_currentAudioDurationSeconds} seconds");

                // ALTERADO: Quadruplicar a resolução para aumentar a granularidade (barras muito mais finas)
                int resolution = 800; // Número de barras no waveform (dobrado de 400 para 800)

                // Ler o buffer completo para encontrar a amplitude máxima
                int sampleCount = (int)(reader.Length / sizeof(float));
                float[] buffer = new float[sampleCount];
                int bytesRead = reader.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    System.Windows.MessageBox.Show("Não foi possível ler dados do arquivo WAV.", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    _currentAudioDurationSeconds = 0;
                    return;
                }

                // --- Encontrar a amplitude máxima real no buffer ---
                float overallMaxAmplitude = 0f;
                for (int i = 0; i < bytesRead; i++)
                {
                    if (Math.Abs(buffer[i]) > overallMaxAmplitude)
                    {
                        overallMaxAmplitude = Math.Abs(buffer[i]);
                    }
                }

                // Calcular o dB correspondente à amplitude máxima real do arquivo
                double scalingMaxDb = 20 * Math.Log10(overallMaxAmplitude + 1e-10);
                // Definir um piso para o dB mínimo para a escala visual
                double scalingMinDb = -60.0; 

                // Proteção contra divisão por zero ou escala extrema para arquivos muito silenciosos
                if (scalingMaxDb <= scalingMinDb + 1e-5) // Adiciona um pequeno epsilon
                {
                    scalingMaxDb = 0.0; // Fallback para uma escala padrão se o áudio for muito baixo/plano
                    scalingMinDb = -60.0;
                }


                double barWidth = width / resolution;
                // Calcular samplesPerBar para pegar o pico de cada segmento
                int samplesPerBar = bytesRead / resolution;
                if (samplesPerBar == 0) samplesPerBar = 1; // Garante que cada barra tenha pelo menos 1 amostra

                // DECLARAÇÃO DE timePerBar - ESTA LINHA É CRUCIAL E DEVE ESTAR AQUI!
                double timePerBar = _currentAudioDurationSeconds / resolution; 

                for (int i = 0; i < resolution; i++)
                {
                    int startIndex = i * samplesPerBar;
                    int endIndex = Math.Min(startIndex + samplesPerBar, bytesRead);

                    // Encontrar a amplitude máxima dentro do segmento de amostras desta barra
                    float maxAmplitudeForBar = 0f;
                    for (int j = startIndex; j < endIndex; j++)
                    {
                        if (Math.Abs(buffer[j]) > maxAmplitudeForBar)
                        {
                            maxAmplitudeForBar = Math.Abs(buffer[j]);
                        }
                    }

                    // Usar a amplitude máxima do segmento para calcular o dB da barra
                    float amplitude = maxAmplitudeForBar; 
                    double db = 20 * Math.Log10(amplitude + 1e-10);
                    
                    // Aplicar a normalização dinâmica baseada no pico real do áudio
                    double percent = (db - scalingMinDb) / (scalingMaxDb - scalingMinDb); 
                    percent = Math.Max(0, Math.Min(1, percent)); // Garante que esteja entre 0 e 1

                    double barHeight = Math.Max(1, percent * height); // Altura mínima reduzida para barras mais finas

                    // Calcular o tempo de início e fim da barra
                    double barStartTime = i * timePerBar; 
                    double barEndTime = (i + 1) * timePerBar; 

                    // Verificar se há sobreposição entre o intervalo da barra e qualquer silêncio
                    bool isSilence = _silencios.Any(s => 
                        barStartTime < s.end.TotalSeconds && barEndTime > s.start.TotalSeconds
                    );

                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = Math.Max(1, barWidth), // Largura mínima reduzida para barras mais finas
                        Height = barHeight,
                        Fill = isSilence
                            ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 0, 0)) // vermelho
                            : System.Windows.Media.Brushes.LightBlue,
                        Stroke = isSilence
                            ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 120, 0, 0)) // vermelho escuro
                            : System.Windows.Media.Brushes.DarkBlue,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(rect, i * barWidth);
                    Canvas.SetTop(rect, (height - barHeight) / 2); // Centraliza verticalmente
                    WaveformCanvas.Children.Add(rect);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao renderizar waveform: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _currentAudioDurationSeconds = 0; // Resetar duração em caso de erro
            }
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            // Remover o evento para evitar múltiplas chamadas
            WaveformCanvas.LayoutUpdated -= OnLayoutUpdated;
            
            // Tentar renderizar novamente
            if (!string.IsNullOrEmpty(_lastWavPath))
            {
                RenderWaveformInternal(_lastWavPath);
            }
        }

        public void DestacarSilencios(List<(TimeSpan start, TimeSpan end)> silencios)
        {
            _silencios = silencios;
            // Re-renderizar o waveform para aplicar o destaque dos silêncios
            if (!string.IsNullOrEmpty(_lastWavPath))
            {
                RenderWaveformInternal(_lastWavPath);
            }
        }

        // Método de teste (mantido como estava, mas as melhorias gerais se aplicam)
        public void RenderTestWaveform()
        {
            try
            {
                WaveformCanvas.Children.Clear();
                // TimeCursor é um elemento x:Name, então ele existe.
                WaveformCanvas.Children.Add(TimeCursor);

                WaveformCanvas.UpdateLayout();
                WaveformCanvas.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                WaveformCanvas.Arrange(new System.Windows.Rect(0, 0, WaveformCanvas.ActualWidth, WaveformCanvas.ActualHeight));

                double width = WaveformCanvas.ActualWidth;
                double height = WaveformCanvas.ActualHeight;

                System.Diagnostics.Debug.WriteLine($"Dimensões do canvas: {width}x{height}");

                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Dimensões do canvas inválidas: {width}x{height}. Aguardando layout...");
                    WaveformCanvas.LayoutUpdated -= OnTestLayoutUpdated; // Evitar múltiplas assinaturas
                    WaveformCanvas.LayoutUpdated += OnTestLayoutUpdated;
                    return;
                }

                // Para o waveform de teste, podemos definir uma duração arbitrária
                _currentAudioDurationSeconds = 10.0; // Exemplo: 10 segundos de áudio de teste

                // ALTERADO: Dobrar a resolução para aumentar a granularidade (barras mais finas)
                int resolution = 800; // Número de barras no waveform (dobrado de 400 para 800)
                double barWidth = width / resolution;

                for (int i = 0; i < resolution; i++)
                {
                    double amplitude = Math.Sin(i * 0.1) * 0.5 + 0.5;
                    double barHeight = Math.Max(1, amplitude * height * 0.8); // Altura mínima reduzida

                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = Math.Max(1, barWidth), // Largura mínima reduzida
                        Height = barHeight,
                        Fill = System.Windows.Media.Brushes.LightBlue,
                        Stroke = System.Windows.Media.Brushes.DarkBlue,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(rect, i * barWidth);
                    Canvas.SetTop(rect, (height - barHeight) / 2);
                    WaveformCanvas.Children.Add(rect);
                }

                System.Diagnostics.Debug.WriteLine($"Waveform de teste renderizado com sucesso! {WaveformCanvas.Children.Count} elementos criados.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao renderizar waveform de teste: {ex.Message}");
            }
        }

        private void OnTestLayoutUpdated(object sender, EventArgs e)
        {
            WaveformCanvas.LayoutUpdated -= OnTestLayoutUpdated;
            RenderTestWaveform();
        }

        public bool IsSilenceAt(TimeSpan time)
        {
            return _silencios.Any(s => time >= s.start && time <= s.end);
        }
    }
}
