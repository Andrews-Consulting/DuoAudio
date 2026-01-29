using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioDuplication.Services
{
    public class AudioCaptureService : IAudioCaptureService, IDisposable
    {
        private WasapiCapture? _capture;
        private string? _deviceId;
        private readonly Queue<byte[]> _audioBuffer = new();
        private readonly object _bufferLock = new();
        private bool _isCapturing;

        public bool IsCapturing => _isCapturing;

        public event EventHandler<byte[]>? DataAvailable;

        public void Initialize(string deviceId)
        {
            _deviceId = deviceId;
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

                // Use WasapiLoopbackCapture for output devices (loopback), WasapiCapture for input devices
                if (device.DataFlow == DataFlow.Render)
                {
                    _capture = new WasapiLoopbackCapture(device);
                }
                else
                {
                    _capture = new WasapiCapture(device);
                }

                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                _capture.StartRecording();
                _isCapturing = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start capture: {ex.Message}", ex);
            }
        }

        public void StopCapture()
        {
            if (!_isCapturing || _capture == null)
                return;

            _capture.StopRecording();
            _isCapturing = false;
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
            if (e.BytesRecorded > 0)
            {
                var buffer = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);

                lock (_bufferLock)
                {
                    _audioBuffer.Enqueue(buffer);
                    // Keep buffer size reasonable (max 10 buffers)
                    while (_audioBuffer.Count > 10)
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
            }
        }

        public void Dispose()
        {
            StopCapture();
            _capture?.Dispose();
            _capture = null;
        }
    }
}
