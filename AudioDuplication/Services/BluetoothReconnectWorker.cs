using System.Timers;
using NAudio.CoreAudioApi;

namespace AudioDuplication.Services
{
    public class BluetoothReconnectWorker : IBluetoothReconnectWorker, IDisposable
    {
        private string? _deviceId;
        private System.Timers.Timer? _monitorTimer;
        private readonly MMDeviceEnumerator _enumerator;
        private bool _isMonitoring;
        private bool _isDeviceConnected;
        private readonly object _lockObject = new();

        public bool IsMonitoring => _isMonitoring;

        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            private set
            {
                if (_isDeviceConnected != value)
                {
                    _isDeviceConnected = value;
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                }
            }
        }

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<string>? ReconnectAttempted;

        public BluetoothReconnectWorker()
        {
            _enumerator = new MMDeviceEnumerator();
        }

        public void StartMonitoring(string deviceId)
        {
            lock (_lockObject)
            {
                if (_isMonitoring)
                    StopMonitoring();

                _deviceId = deviceId;

                // Check initial connection status
                CheckDeviceConnection();

                // Start monitoring timer (check every 5 seconds)
                _monitorTimer = new System.Timers.Timer(5000);
                _monitorTimer.Elapsed += OnTimerElapsed;
                _monitorTimer.AutoReset = true;
                _monitorTimer.Start();

                _isMonitoring = true;
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring)
                    return;

                _monitorTimer?.Stop();
                _monitorTimer?.Dispose();
                _monitorTimer = null;

                _isMonitoring = false;
                _deviceId = null;
            }
        }

        public void AttemptReconnect()
        {
            if (string.IsNullOrEmpty(_deviceId))
                return;

            ReconnectAttempted?.Invoke(this, $"Attempting to reconnect device: {_deviceId}");

            try
            {
                // Check if device is available
                var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var device = devices.FirstOrDefault(d => d.ID == _deviceId);

                if (device != null)
                {
                    IsDeviceConnected = true;
                    ReconnectAttempted?.Invoke(this, "Device reconnected successfully");
                }
                else
                {
                    // Try to find by friendly name (in case ID changed)
                    var deviceName = GetDeviceNameFromHistory(_deviceId);
                    if (!string.IsNullOrEmpty(deviceName))
                    {
                        device = devices.FirstOrDefault(d => d.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase));
                        if (device != null)
                        {
                            _deviceId = device.ID; // Update to new ID
                            IsDeviceConnected = true;
                            ReconnectAttempted?.Invoke(this, "Device reconnected with new ID");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReconnectAttempted?.Invoke(this, $"Reconnect failed: {ex.Message}");
            }
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            CheckDeviceConnection();

            // If device is disconnected and we're monitoring, attempt reconnect
            if (!IsDeviceConnected && _isMonitoring)
            {
                AttemptReconnect();
            }
        }

        private void CheckDeviceConnection()
        {
            if (string.IsNullOrEmpty(_deviceId))
            {
                IsDeviceConnected = false;
                return;
            }

            try
            {
                var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var isConnected = devices.Any(d => d.ID == _deviceId);

                // Also check by name if ID match fails
                if (!isConnected)
                {
                    var deviceName = GetDeviceNameFromHistory(_deviceId);
                    if (!string.IsNullOrEmpty(deviceName))
                    {
                        isConnected = devices.Any(d => d.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase));
                    }
                }

                IsDeviceConnected = isConnected;
            }
            catch
            {
                IsDeviceConnected = false;
            }
        }

        private string? GetDeviceNameFromHistory(string deviceId)
        {
            try
            {
                // Try to get the device name from all devices (including disconnected)
                var allDevices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All);
                var device = allDevices.FirstOrDefault(d => d.ID == deviceId);
                return device?.FriendlyName;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _enumerator?.Dispose();
        }
    }
}
