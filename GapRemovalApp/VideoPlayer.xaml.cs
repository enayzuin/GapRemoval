using System;
using System.Windows.Controls;

namespace GapRemovalApp
{
    public partial class VideoPlayer : UserControl
    {
        public VideoPlayer()
        {
            InitializeComponent();
        }

        public void LoadVideo(string videoPath)
        {
            Player.Source = new Uri(videoPath);
            Player.Play();
        }

        public TimeSpan GetDuration() => Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

        public TimeSpan GetPosition() => Player.Position;

        public void Seek(TimeSpan time) => Player.Position = time;
    }
}