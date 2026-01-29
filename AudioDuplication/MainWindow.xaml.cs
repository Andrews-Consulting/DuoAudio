using System.Windows;
using AudioDuplication.Services;
using AudioDuplication.ViewModels;

namespace AudioDuplication;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AudioDuplicationViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        InitializeViewModel();
    }

    private void InitializeViewModel()
    {
        // Create services
        var deviceEnumerator = new AudioDeviceEnumerator();
        var bluetoothReconnectWorker = new BluetoothReconnectWorker();

        // Create ViewModel with proper service initialization
        _viewModel = new AudioDuplicationViewModel(deviceEnumerator, null, bluetoothReconnectWorker);

        // Wire up device selection changes to create new duplication worker
        SourceDeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (SourceDeviceComboBox.SelectedItem != null)
            {
                _viewModel.SelectedSourceDevice = SourceDeviceComboBox.SelectedItem as Models.AudioDeviceInfo;
                UpdateDuplicationWorker();
            }
        };

        DestinationDeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (DestinationDeviceComboBox.SelectedItem != null)
            {
                _viewModel.SelectedDestinationDevice = DestinationDeviceComboBox.SelectedItem as Models.AudioDeviceInfo;
                UpdateDuplicationWorker();
            }
        };

        DataContext = _viewModel;

        // Populate device dropdowns
        LoadDevices();

        // Subscribe to device changes
        deviceEnumerator.SubscribeToDeviceChanges(LoadDevices);

        // Wire up button events
        StartButton.Click += (s, e) => _viewModel.StartDuplication();
        StopButton.Click += (s, e) => _viewModel.StopDuplication();

        // Wire up checkbox events
        EnableDuplicationCheckBox.Checked += (s, e) => _viewModel.IsDuplicationEnabled = true;
        EnableDuplicationCheckBox.Unchecked += (s, e) => _viewModel.IsDuplicationEnabled = false;
        AutoReconnectCheckBox.Checked += (s, e) => _viewModel.AutoReconnectEnabled = true;
        AutoReconnectCheckBox.Unchecked += (s, e) => _viewModel.AutoReconnectEnabled = false;

        // Wire up selection changed events
        SourceDeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (SourceDeviceComboBox.SelectedItem != null)
                _viewModel.SelectedSourceDevice = SourceDeviceComboBox.SelectedItem as Models.AudioDeviceInfo;
        };

        DestinationDeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (DestinationDeviceComboBox.SelectedItem != null)
                _viewModel.SelectedDestinationDevice = DestinationDeviceComboBox.SelectedItem as Models.AudioDeviceInfo;
        };

        // Bind status
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AudioDuplicationViewModel.Status))
            {
                Dispatcher.Invoke(() => StatusTextBlock.Text = $"Status: {_viewModel.Status}");
            }
            else if (e.PropertyName == nameof(AudioDuplicationViewModel.IsBluetoothConnected))
            {
                Dispatcher.Invoke(() => BluetoothStatusTextBlock.Text =
                    $"BT Status: {(_viewModel.IsBluetoothConnected ? "Connected" : "Disconnected")}");
            }
        };

        // Wire up Bluetooth reconnect events
        AutoReconnectCheckBox.Checked += (s, e) => _viewModel.AutoReconnectEnabled = true;
        AutoReconnectCheckBox.Unchecked += (s, e) => _viewModel.AutoReconnectEnabled = false;
    }

    private void LoadDevices()
    {
        Dispatcher.Invoke(() =>
        {
            if (_viewModel == null) return;

            // Store current selections
            var currentSource = SourceDeviceComboBox.SelectedItem;
            var currentDestination = DestinationDeviceComboBox.SelectedItem;

            // Reload devices
            _viewModel.SourceDevices = _viewModel.SourceDevices; // Trigger reload
            _viewModel.DestinationDevices = _viewModel.DestinationDevices; // Trigger reload

            // Repopulate dropdowns
            SourceDeviceComboBox.ItemsSource = _viewModel.SourceDevices;
            DestinationDeviceComboBox.ItemsSource = _viewModel.DestinationDevices;

            // Restore selections if possible
            if (currentSource != null)
                SourceDeviceComboBox.SelectedItem = currentSource;
            if (currentDestination != null)
                DestinationDeviceComboBox.SelectedItem = currentDestination;
        });
    }

    /// <summary>
    /// Creates or updates the duplication worker when device selection changes
    /// </summary>
    private void UpdateDuplicationWorker()
    {
        if (_viewModel?.SelectedSourceDevice == null || _viewModel?.SelectedDestinationDevice == null)
            return;

        // Stop any existing duplication
        _viewModel.StopDuplication();

        // Create new capture and playback services with selected devices
        var captureService = new AudioCaptureService();
        captureService.Initialize(_viewModel.SelectedSourceDevice.Id);

        var playbackService = new AudioPlaybackService();
        playbackService.Initialize(_viewModel.SelectedDestinationDevice.Id);

        // Create new duplication worker
        var duplicationWorker = new AudioDuplicationWorker(captureService, playbackService);

        // Update ViewModel with new worker
        _viewModel.GetType()
            .GetProperty("DuplicationWorker")
            ?.SetValue(_viewModel, duplicationWorker);
    }
}