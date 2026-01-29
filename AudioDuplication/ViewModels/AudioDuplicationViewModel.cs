using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioDuplication.Models;
using AudioDuplication.Services;

namespace AudioDuplication.ViewModels
{
    public class AudioDuplicationViewModel : INotifyPropertyChanged
    {
        private readonly IAudioDeviceEnumerator _deviceEnumerator;
        private IAudioDuplicationWorker? _duplicationWorker;
        private readonly IBluetoothReconnectWorker? _bluetoothReconnectWorker;

        private List<AudioDeviceInfo> _sourceDevices = new();
        private List<AudioDeviceInfo> _destinationDevices = new();
        private AudioDeviceInfo? _selectedSourceDevice;
        private AudioDeviceInfo? _selectedDestinationDevice;
        private bool _isDuplicationEnabled;
        private bool _isRunning;
        private string _status = "Idle";
        private bool _autoReconnectEnabled;
        private bool _isBluetoothConnected;

        public AudioDuplicationViewModel(
            IAudioDeviceEnumerator deviceEnumerator,
            IAudioDuplicationWorker? duplicationWorker,
            IBluetoothReconnectWorker? bluetoothReconnectWorker = null)
        {
            _deviceEnumerator = deviceEnumerator;
            _duplicationWorker = duplicationWorker;
            _bluetoothReconnectWorker = bluetoothReconnectWorker;

            if (_duplicationWorker != null)
            {
                _duplicationWorker.StatusChanged += (s, status) => Status = status;
            }

            if (_bluetoothReconnectWorker != null)
            {
                _bluetoothReconnectWorker.ConnectionStatusChanged += (s, connected) =>
                {
                    IsBluetoothConnected = connected;
                };
            }

            LoadDevices();
        }

        public IAudioDuplicationWorker? DuplicationWorker
        {
            get => _duplicationWorker;
            set
            {
                // Unsubscribe from old worker
                if (_duplicationWorker != null)
                {
                    _duplicationWorker.StatusChanged -= OnDuplicationStatusChanged;
                }

                _duplicationWorker = value;

                // Subscribe to new worker
                if (_duplicationWorker != null)
                {
                    _duplicationWorker.StatusChanged += OnDuplicationStatusChanged;
                }

                OnPropertyChanged();
            }
        }

        private void OnDuplicationStatusChanged(object? sender, string status)
        {
            Status = status;
        }

        public List<AudioDeviceInfo> SourceDevices
        {
            get => _sourceDevices;
            set
            {
                _sourceDevices = value;
                OnPropertyChanged();
            }
        }

        public List<AudioDeviceInfo> DestinationDevices
        {
            get => _destinationDevices;
            set
            {
                _destinationDevices = value;
                OnPropertyChanged();
            }
        }

        public AudioDeviceInfo? SelectedSourceDevice
        {
            get => _selectedSourceDevice;
            set
            {
                _selectedSourceDevice = value;
                OnPropertyChanged();
            }
        }

        public AudioDeviceInfo? SelectedDestinationDevice
        {
            get => _selectedDestinationDevice;
            set
            {
                _selectedDestinationDevice = value;
                OnPropertyChanged();
            }
        }

        public bool IsDuplicationEnabled
        {
            get => _isDuplicationEnabled;
            set
            {
                _isDuplicationEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool AutoReconnectEnabled
        {
            get => _autoReconnectEnabled;
            set
            {
                _autoReconnectEnabled = value;
                OnPropertyChanged();
                OnAutoReconnectChanged();
            }
        }

        public bool IsBluetoothConnected
        {
            get => _isBluetoothConnected;
            set
            {
                _isBluetoothConnected = value;
                OnPropertyChanged();
            }
        }

        public void StartDuplication()
        {
            if (!IsDuplicationEnabled)
            {
                Status = "Error: Duplication not enabled";
                return;
            }

            if (SelectedSourceDevice == null || SelectedDestinationDevice == null)
            {
                Status = "Error: Please select both source and destination devices";
                return;
            }

            if (_duplicationWorker == null)
            {
                Status = "Error: Duplication worker not initialized";
                return;
            }

            try
            {
                _duplicationWorker.Start();
                IsRunning = _duplicationWorker.IsRunning;
                Status = "Active";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                IsRunning = false;
            }
        }

        public void StopDuplication()
        {
            if (_duplicationWorker == null)
            {
                Status = "Idle";
                return;
            }

            try
            {
                _duplicationWorker.Stop();
                IsRunning = _duplicationWorker.IsRunning;
                Status = "Idle";
            }
            catch (Exception ex)
            {
                Status = $"Error stopping: {ex.Message}";
                IsRunning = false;
            }
        }

        private void LoadDevices()
        {
            // Source should be output devices (for loopback capture of system audio)
            SourceDevices = _deviceEnumerator.GetOutputDevices();
            DestinationDevices = _deviceEnumerator.GetOutputDevices();
        }

        private void OnAutoReconnectChanged()
        {
            if (_bluetoothReconnectWorker == null) return;

            if (AutoReconnectEnabled && SelectedDestinationDevice != null)
            {
                _bluetoothReconnectWorker.StartMonitoring(SelectedDestinationDevice.Id);
            }
            else
            {
                _bluetoothReconnectWorker.StopMonitoring();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
