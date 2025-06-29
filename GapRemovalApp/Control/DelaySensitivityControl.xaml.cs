using System;
using System.Windows;
using System.Windows.Controls;

namespace GapRemovalApp
{
    public partial class DelaySensitivityControl : System.Windows.Controls.UserControl
    {
        public event Action<int>? DelayChanged;

        public DelaySensitivityControl()
        {
            InitializeComponent();
            DelaySlider.Value = 700;
            DelayValueLabel.Text = "700 ms";
        }

        public int CurrentDelay => (int)DelaySlider.Value;

        private void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DelayValueLabel != null) { 
            int newDelay = (int)e.NewValue;
            DelayValueLabel.Text = $"{newDelay} ms";  // Atualizando o valor do delay na interface
            DelayChanged?.Invoke(newDelay);
        }
        }
    }
}