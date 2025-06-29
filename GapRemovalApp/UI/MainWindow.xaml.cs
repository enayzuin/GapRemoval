using System.Windows;
using GapRemovalApp.UI;

namespace GapRemovalApp.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenEditor_Click(object sender, RoutedEventArgs e)
        {
            var editor = new SilentCutterWindow();
            editor.Show();
            Close();
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new VideoProcessingConfigForm();
            configWindow.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }
    }
}
