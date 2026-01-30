using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using DuoAudio.Services;
using DuoAudio.ViewModels;

namespace DuoAudio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DuoAudioViewModel? _viewModel;
    private ISystemTrayService? _systemTrayService;
    private IStartupManager? _startupManager;
    private ICommandLineParser? _commandLineParser;
    private ISingleInstanceManager? _singleInstanceManager;
    private NotifyIcon? _notifyIcon;
    private readonly string[] _args;

    public MainWindow(string[] args)
    {
        try
        {
            _args = args ?? Array.Empty<string>();
            System.Diagnostics.Debug.WriteLine("MainWindow constructor called");

            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("InitializeComponent completed");

            InitializeServices();
            System.Diagnostics.Debug.WriteLine("InitializeServices completed");

            InitializeViewModel();
            System.Diagnostics.Debug.WriteLine("InitializeViewModel completed");

            ProcessCommandLine();
            System.Diagnostics.Debug.WriteLine("ProcessCommandLine completed");

            InitializeSystemTray();
            System.Diagnostics.Debug.WriteLine("InitializeSystemTray completed");

            CheckStartupStatus();
            System.Diagnostics.Debug.WriteLine("CheckStartupStatus completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MainWindow constructor: {ex.Message}\n{ex.StackTrace}");
            System.Windows.MessageBox.Show($"Error initializing application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void InitializeServices()
    {
        _systemTrayService = new SystemTrayService();
        _startupManager = new StartupManager();
        _commandLineParser = new CommandLineParser();
        _singleInstanceManager = new SingleInstanceManager();
        _singleInstanceManager.Initialize();

        if (!_singleInstanceManager.IsFirstInstance)
        {
            _singleInstanceManager.SignalExistingInstance();
            System.Windows.Application.Current.Shutdown();
            return;
        }

        // Wait for signal from another instance
        _singleInstanceManager.WaitForShowSignal(() =>
        {
            Dispatcher.Invoke(() =>
            {
                ShowWindow();
            });
        });
    }

    private void InitializeViewModel()
    {
        // Create services
        var deviceEnumerator = new AudioDeviceEnumerator();
        var bluetoothReconnectWorker = new BluetoothReconnectWorker();

        // Create ViewModel with proper service initialization
        _viewModel = new DuoAudioViewModel(deviceEnumerator, null, bluetoothReconnectWorker);

        // Wire up device selection changes to create new duplication worker
        SourceDeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (SourceDeviceComboBox.SelectedItem != null)
            {
                _viewModel.SelectedSourceDevice = SourceDeviceComboBox.SelectedItem as Models.AudioDeviceInfo;
                UpdateDuplicationWorker();
                UpdateRunAtStartupCheckbox();
            }
        };

        DestinationDeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (DestinationDeviceComboBox.SelectedItem != null)
            {
                _viewModel.SelectedDestinationDevice = DestinationDeviceComboBox.SelectedItem as Models.AudioDeviceInfo;
                UpdateDuplicationWorker();
                UpdateRunAtStartupCheckbox();
            }
        };

        DataContext = _viewModel;

        // Populate device dropdowns
        LoadDevices();

        // Subscribe to device changes
        deviceEnumerator.SubscribeToDeviceChanges(LoadDevices);

        // Wire up buffer configuration slider
        BufferConfigSlider.ValueChanged += (s, e) =>
        {
            var config = (int)BufferConfigSlider.Value;
            BufferConfigValue.Text = GetBufferConfigLabel(config);
            
            // Update buffer configuration in services if worker is running
            if (_viewModel?.DuplicationWorker != null)
            {
                UpdateBufferConfiguration(config);
            }
        };

        // Wire up checkbox events
        AutoReconnectCheckBox.Checked += (s, e) => _viewModel.AutoReconnectEnabled = true;
        AutoReconnectCheckBox.Unchecked += (s, e) => _viewModel.AutoReconnectEnabled = false;

        // Bind status
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DuoAudioViewModel.Status))
            {
                Dispatcher.Invoke(() => StatusTextBlock.Text = $"Status: {_viewModel.Status}");
            }
            else if (e.PropertyName == nameof(DuoAudioViewModel.IsBluetoothConnected))
            {
                Dispatcher.Invoke(() => BluetoothStatusTextBlock.Text =
                    $"BT Status: {(_viewModel.IsBluetoothConnected ? "Connected" : "Disconnected")}");
            }
            else if (e.PropertyName == nameof(DuoAudioViewModel.IsRunning))
            {
                Dispatcher.Invoke(() => UpdateRunAtStartupCheckbox());
            }
        };
    }

    private void InitializeSystemTray()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = false,
            Text = "DuoAudio"
        };

        _systemTrayService?.Initialize(_notifyIcon);
        _systemTrayService?.SetOnClickAction(() =>
        {
            ShowWindow();
        });
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _systemTrayService?.Hide();
    }

    private void HideToTray()
    {
        Hide();
        _systemTrayService?.Show();
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // If worker is running, hide to tray instead of closing
        if (_viewModel?.IsRunning == true)
        {
            e.Cancel = true;
            HideToTray();
        }
        else
        {
            _systemTrayService?.Dispose();
        }
    }

    private void ProcessCommandLine()
    {
        var config = _commandLineParser?.Parse(_args);

        if (config?.SourceDeviceId != null && config?.DestinationDeviceId != null)
        {
            // Load devices and select them
            LoadDevicesAndSelect(config.SourceDeviceId, config.DestinationDeviceId);
        }
    }

    private void LoadDevicesAndSelect(string sourceId, string destinationId)
    {
        if (_viewModel == null) return;

        try
        {
            // Load devices
            _viewModel.SourceDevices = _viewModel.SourceDevices;
            _viewModel.DestinationDevices = _viewModel.DestinationDevices;

            // Find devices by ID
            var sourceDevice = _viewModel.SourceDevices.FirstOrDefault(d => d.Id == sourceId);
            var destinationDevice = _viewModel.DestinationDevices.FirstOrDefault(d => d.Id == destinationId);

            if (sourceDevice != null && destinationDevice != null)
            {
                // Select devices
                _viewModel.SelectedSourceDevice = sourceDevice;
                _viewModel.SelectedDestinationDevice = destinationDevice;

                // Update dropdowns
                SourceDeviceComboBox.ItemsSource = _viewModel.SourceDevices;
                DestinationDeviceComboBox.ItemsSource = _viewModel.DestinationDevices;
                SourceDeviceComboBox.SelectedItem = sourceDevice;
                DestinationDeviceComboBox.SelectedItem = destinationDevice;

                // Start duplication automatically
                _viewModel.StartDuplication();
                UpdateWorkerUI(_viewModel.IsRunning);

                // Hide to tray if running
                if (_viewModel.IsRunning)
                {
                    HideToTray();
                }
            }
            else
            {
                // Show error if devices not found
                _viewModel.Status = "Error: Configured devices not found. Please select devices manually.";
                ShowWindow();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading devices from command line: {ex.Message}");
            _viewModel.Status = $"Error loading devices: {ex.Message}";
            ShowWindow();
        }
    }

    private void CheckStartupStatus()
    {
        var isInStartup = _startupManager?.IsInStartup() ?? false;
        RunAtStartupCheckBox.IsChecked = isInStartup;
        UpdateRunAtStartupCheckbox();
    }

    private void OnRunAtStartupChecked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedSourceDevice == null || _viewModel?.SelectedDestinationDevice == null)
        {
            RunAtStartupCheckBox.IsChecked = false;
            return;
        }

        try
        {
            _startupManager?.AddToStartup(
                _viewModel.SelectedSourceDevice.Id,
                _viewModel.SelectedDestinationDevice.Id
            );
            _viewModel.RunAtStartup = true;
            System.Diagnostics.Debug.WriteLine("Added to startup");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding to startup: {ex.Message}");
            _viewModel.Status = $"Error adding to startup: {ex.Message}";
            RunAtStartupCheckBox.IsChecked = false;
        }
    }

    private void OnRunAtStartupUnchecked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _startupManager?.RemoveFromStartup();
            if (_viewModel != null)
            {
                _viewModel.RunAtStartup = false;
            }
            System.Diagnostics.Debug.WriteLine("Removed from startup");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing from startup: {ex.Message}");
            if (_viewModel != null)
            {
                _viewModel.Status = $"Error removing from startup: {ex.Message}";
            }
        }
    }

    private void UpdateRunAtStartupCheckbox()
    {
        RunAtStartupCheckBox.IsEnabled =
            _viewModel?.SelectedSourceDevice != null &&
            _viewModel?.SelectedDestinationDevice != null &&
            _viewModel?.IsRunning == true;
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

            try
            {
                // Stop any existing duplication
                _viewModel.StopDuplication();

                // Get buffer configuration from slider
                var bufferConfig = (int)BufferConfigSlider.Value;

                // Calculate ring buffer size based on configuration
                // Default to 48kHz, stereo, 16-bit (4 bytes per sample)
                int sampleRate = 48000;
                int channels = 2;
                int bytesPerSample = 2;
                int bufferDurationMs = GetBufferDurationForConfig(bufferConfig);
                int bufferSize = (sampleRate * channels * bytesPerSample * bufferDurationMs) / 1000;

                // Create shared ring buffer
                var ringBuffer = new AudioRingBuffer(bufferSize);

                // Create new capture and playback services with selected devices and shared ring buffer
                var captureService = new AudioCaptureService();
                captureService.Initialize(_viewModel.SelectedSourceDevice.Id, ringBuffer);

                var playbackService = new AudioPlaybackService();
                playbackService.Initialize(_viewModel.SelectedDestinationDevice.Id, ringBuffer);

                // Create new duplication worker
                var duplicationWorker = new AudioDuplicationWorker(captureService, playbackService);

                // Update ViewModel with new worker using the new method
                _viewModel.SetDuplicationWorker(duplicationWorker);

                System.Diagnostics.Debug.WriteLine($"Duplication worker created successfully for source: {_viewModel.SelectedSourceDevice.Name}, destination: {_viewModel.SelectedDestinationDevice.Name}, buffer config: {GetBufferConfigLabel(bufferConfig)}, buffer size: {bufferSize} bytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating duplication worker: {ex.Message}");
                _viewModel.Status = $"Error initializing worker: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the buffer duration in milliseconds for a given configuration
        /// </summary>
        private int GetBufferDurationForConfig(int config)
        {
            return config switch
            {
                1 => 20,   // Low Latency
                2 => 50,   // Low-Medium
                3 => 100,  // Balanced (default)
                4 => 200,  // Medium-High
                5 => 500,  // High Stability
                _ => 100    // Default to balanced
            };
        }

    /// <summary>
    /// Handles the toggle button click to start or stop the worker
    /// </summary>
    private void OnToggleWorkerButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnToggleWorkerButtonClick called");

            if (_viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("ViewModel is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"IsRunning: {_viewModel.IsRunning}");

            if (_viewModel.IsRunning)
            {
                // Stop the worker
                System.Diagnostics.Debug.WriteLine("Stopping worker");
                _viewModel.StopDuplication();
                UpdateWorkerUI(false);
            }
            else
            {
                // Start the worker
                System.Diagnostics.Debug.WriteLine("Starting worker");
                _viewModel.StartDuplication();
                UpdateWorkerUI(_viewModel.IsRunning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnToggleWorkerButtonClick: {ex.Message}");
            if (_viewModel != null)
            {
                _viewModel.Status = $"Error: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Updates the UI to reflect worker status
    /// </summary>
    private void UpdateWorkerUI(bool isRunning)
    {
        Dispatcher.Invoke(() =>
        {
            if (isRunning)
            {
                ToggleWorkerButton.Content = "Stop Worker Task";
                ToggleWorkerButton.Background = System.Windows.Media.Brushes.LightCoral;
                WorkerStatusTextBlock.Text = "Worker Status: Running";
                WorkerStatusBorder.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                ToggleWorkerButton.Content = "Start Worker Task";
                ToggleWorkerButton.Background = System.Windows.Media.Brushes.LightGreen;
                WorkerStatusTextBlock.Text = "Worker Status: Stopped";
                WorkerStatusBorder.Background = System.Windows.Media.Brushes.LightGray;
            }
        });
    }

    /// <summary>
    /// Opens the about window
    /// </summary>
    private void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var bufferConfig = (int)BufferConfigSlider.Value;
        var aboutWindow = new AboutWindow(bufferConfig);
        aboutWindow.Owner = this;
        aboutWindow.Show();
    }

    /// <summary>
    /// Gets the label for the buffer configuration slider
    /// </summary>
    private string GetBufferConfigLabel(int config)
    {
        return config switch
        {
            1 => "Low Latency",
            2 => "Low-Medium",
            3 => "Balanced",
            4 => "Medium-High",
            5 => "High Stability",
            _ => "Balanced"
        };
    }

    /// <summary>
    /// Updates buffer configuration in the audio services
    /// </summary>
    private void UpdateBufferConfiguration(int config)
    {
        // This will be implemented when services support dynamic buffer configuration
        // For now, the configuration is applied when services are initialized
        System.Diagnostics.Debug.WriteLine($"Buffer configuration changed to: {GetBufferConfigLabel(config)}");
    }
}
