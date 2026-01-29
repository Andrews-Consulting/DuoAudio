using System.Windows;

namespace DuoAudio
{
    public partial class AboutWindow : Window
    {
        private readonly int _bufferConfiguration;

        public AboutWindow(int bufferConfiguration = 3)
        {
            InitializeComponent();
            _bufferConfiguration = bufferConfiguration;
        }

        private void OnOpenDiagnosticsClick(object sender, RoutedEventArgs e)
        {
            var diagWindow = new DiagnosticsWindow();
            diagWindow.Owner = this.Owner;
            diagWindow.SetBufferConfiguration(_bufferConfiguration);
            diagWindow.Show();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
