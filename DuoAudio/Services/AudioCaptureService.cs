using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DuoAudio.Services
{
    public class AudioCaptureService : IAudioCaptureService, IDisposable
    {
        private WasapiCapture? _capture;
        private string? _deviceId;
        private AudioRingBuffer? _ringBuffer;
        private bool _isCapturing;
        private bool _isDisposed;

        public bool IsCapturing => _isCapturing;

        public event EventHandler<Exception>? CaptureError;
        public event EventHandler? DeviceDisconnected;

        public void Initialize(string deviceId)
        {
            throw new InvalidOperationException("Use Initialize(string deviceId, AudioRingBuffer ringBuffer) instead.");
        }

        public void Initialize(string deviceId, AudioRingBuffer ringBuffer)
        {
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _ringBuffer = ringBuffer ?? throw new ArgumentNullException(nameof(ringBuffer));
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

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded > 0 && e.Buffer != null && _ringBuffer != null)
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

                // Write directly to ring buffer - no per-chunk allocation!
                int bytesWritten = _ringBuffer.Write(e.Buffer, 0, e.BytesRecorded);

                if (bytesWritten < e.BytesRecorded)
                {
                    // Buffer overflow - some data was discarded
                    System.Diagnostics.Debug.WriteLine($"Ring buffer overflow: wrote {bytesWritten}/{e.BytesRecorded} bytes");
                }
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
