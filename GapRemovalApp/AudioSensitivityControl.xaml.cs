using System;
using System.Windows;
using System.Windows.Controls;

namespace GapRemovalApp
{
    public partial class AudioSensitivityControl : UserControl
    {
        public event Action<float>? SensitivityChanged;

        public AudioSensitivityControl()
        {
            InitializeComponent();
        }

        public float CurrentValue => (float)SensitivitySlider.Value;

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float newValue = (float)e.NewValue;
            if (ValueLabel != null)
                ValueLabel.Text = $"{newValue:0} dB";

            SensitivityChanged?.Invoke(newValue);
        }
    }
}
