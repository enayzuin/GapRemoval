using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using Vlc.DotNet.Core;
using Vlc.DotNet.Forms;
using GapRemovalApp.Utils;

namespace GapRemovalApp
{
    public partial class VlcPlayerControl : System.Windows.Controls.UserControl
    {
        private readonly VlcControl vlcControl;

        public VlcPlayerControl()
        {
            InitializeComponent();
            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc");

            vlcControl = new VlcControl();
            vlcControl.VlcLibDirectory = new DirectoryInfo(@vlcPath);
            vlcControl.EndInit();

            // Ativar captura de logs
            vlcControl.Log += VlcControl_Log;

            var host = new WindowsFormsHost
            {
                Child = vlcControl
            };

            MainGrid.Children.Add(host);
        }

        private void VlcControl_Log(object? sender, VlcMediaPlayerLogEventArgs e)
        {
            var logMsg = $"[VLC][{e.Level}] {e.Message}";
            Logger.Debug(logMsg); // seu sistema de logs próprio com Serilog
        }

        public void LoadVideo(string path)
        {
            if (File.Exists(path))
            {
                vlcControl.Play(new Uri(path));
            }
            else
            {
                System.Windows.MessageBox.Show("Arquivo não encontrado: " + path);
            }
        }

        public void Pause()
        {
            if (vlcControl.IsPlaying)
                vlcControl.Pause();
        }

        public void Play()
        {
            if (!vlcControl.IsPlaying)
                vlcControl.Play();
        }

        public void Seek(TimeSpan position)
        {
            vlcControl.Time = (long)position.TotalMilliseconds;
        }

        public TimeSpan GetPosition()
        {
            var ms = vlcControl.Time;
            Logger.Debug($"[VLC] GetPosition: {ms} ms ({TimeSpan.FromMilliseconds(ms)})");
            return TimeSpan.FromMilliseconds(ms);
        }

        public TimeSpan GetDuration()
        {
            return TimeSpan.FromMilliseconds(vlcControl.Length);
        }
    }
}
