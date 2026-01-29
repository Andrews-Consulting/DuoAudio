using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DuoAudio.Services
{
    public class AudioCaptureService : IAudioCaptureService, IDisposable
    {
        private WasapiCapture? _capture;
        private string? _deviceId;
        private readonly Queue<byte[]> _audioBuffer = new();
        private readonly object _bufferLock = new();
        private bool _isCapturing;
        private bool _isDisposed;
        private int _bufferConfig = 3; // Default to balanced

        public bool IsCapturing => _isCapturing;

        public event EventHandler<byte[]>? DataAvailable;
        public event EventHandler<Exception>? CaptureError;
        public event EventHandler? DeviceDisconnected;

        public void Initialize(string deviceId)
        {
            Initialize(deviceId, 3); // Default to balanced
        }

        public void Initialize(string deviceId, int bufferConfig)
        {
            _deviceId = deviceId;
            _bufferConfig = bufferConfig;
        }

        public void StartCapture()
        {
            if (string.IsNullOrEmpty(_deviceId))
                throw new InvalidOperationException("Device not initialized. Call Initialize() first.");

            if (_isCapturing)
                return;

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDevice(_deviceId);

                // Check if device is still available
                if (device.State == DeviceState.NotPresent || device.State == DeviceState.Disabled)
                {
                    throw new InvalidOperationException($"Device is not available. State: {device.State}");
                }

                // Use WasapiLoopbackCapture for output devices (loopback)
                // This captures the audio being sent to the output device
                _capture = new WasapiLoopbackCapture(device);

                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                _capture.StartRecording();
                _isCapturing = true;
                
                System.Diagnostics.Debug.WriteLine($"Capture started successfully for device: {_deviceId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start capture: {ex.Message}");
                CaptureError?.Invoke(this, ex);
                throw new InvalidOperationException($"Failed to start capture: {ex.Message}", ex);
            }
        }

        public void StopCapture()
        {
            if (!_isCapturing || _capture == null)
                return;

            try
            {
                _capture.StopRecording();
                _isCapturing = false;
                System.Diagnostics.Debug.WriteLine("Capture stopped successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping capture: {ex.Message}");
                CaptureError?.Invoke(this, ex);
            }
        }

        public byte[]? GetAudioBuffer()
        {
            lock (_bufferLock)
            {
                return _audioBuffer.Count > 0 ? _audioBuffer.Dequeue() : null;
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded > 0 && e.Buffer != null)
            {
                // Safety check: prevent buffer overflow
                if (e.BytesRecorded > e.Buffer.Length)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: BytesRecorded ({e.BytesRecorded}) exceeds buffer length ({e.Buffer.Length})");
                    return;
                }

                // Limit buffer size to prevent memory issues
                const int maxBufferSize = 1024 * 1024; // 1MB max
                if (e.BytesRecorded > maxBufferSize)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Buffer size ({e.BytesRecorded}) exceeds maximum ({maxBufferSize})");
                    return;
                }

                var buffer = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);

                lock (_bufferLock)
                {
                    _audioBuffer.Enqueue(buffer);
                    // Keep buffer size based on configuration
                    var maxBuffers = GetMaxBuffersForConfig(_bufferConfig);
                    while (_audioBuffer.Count > maxBuffers)
                    {
                        _audioBuffer.Dequeue();
                    }
                }

                DataAvailable?.Invoke(this, buffer);
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            _isCapturing = false;

            if (e.Exception != null)
            {
                // Log or handle the exception
                System.Diagnostics.Debug.WriteLine($"Recording stopped with error: {e.Exception.Message}");
                CaptureError?.Invoke(this, e.Exception);
                
                // Check if it's a device disconnection error
                if (IsDeviceDisconnectionError(e.Exception))
                {
                    System.Diagnostics.Debug.WriteLine("Device disconnected detected");
                    DeviceDisconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool IsDeviceDisconnectionError(Exception ex)
        {
            if (ex == null) return false;
            
            // Check for common device disconnection error messages
            var errorMessage = ex.Message.ToLower();
            return errorMessage.Contains("device") && 
                   (errorMessage.Contains("disconnected") || 
                    errorMessage.Contains("not available") ||
                    errorMessage.Contains("not present") ||
                    errorMessage.Contains("invalid"));
        }

        private int GetMaxBuffersForConfig(int config)
        {
            return config switch
            {
                1 => 2,  // Low Latency
                2 => 2,  // Low-Medium
                3 => 3,  // Balanced (default)
                4 => 5,  // Medium-High
                5 => 10, // High Stability
                _ => 3   // Default to balanced
            };
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            StopCapture();
            
            if (_capture != null)
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.RecordingStopped -= OnRecordingStopped;
                _capture.Dispose();
                _capture = null;
            }
            
            _isDisposed = true;
        }
    }
}
