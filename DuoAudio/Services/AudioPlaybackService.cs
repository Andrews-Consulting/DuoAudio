using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DuoAudio.Services
{
    public class AudioPlaybackService : IAudioPlaybackService, IDisposable
    {
        private WasapiOut? _wasapiOut;
        private RingBufferWaveProvider? _ringBufferWaveProvider;
        private AudioRingBuffer? _ringBuffer;
        private string? _deviceId;
        private bool _isPlaying;
        private readonly object _lockObject = new();
        private bool _isDisposed;

        public bool IsPlaying => _isPlaying;

        public event EventHandler<Exception>? PlaybackError;
#pragma warning disable CS0067 // Event is never used - required by interface
        public event EventHandler? DeviceDisconnected;
#pragma warning restore CS0067

        public void Initialize(string deviceId)
        {
            throw new InvalidOperationException("Use Initialize(string deviceId, AudioRingBuffer ringBuffer) instead.");
        }

        public void Initialize(string deviceId, AudioRingBuffer ringBuffer)
        {
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _ringBuffer = ringBuffer ?? throw new ArgumentNullException(nameof(ringBuffer));
        }

        public void StartPlayback()
        {
            if (string.IsNullOrEmpty(_deviceId))
                throw new InvalidOperationException("Device not initialized. Call Initialize() first.");

            if (_isPlaying)
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

                // Get the device's actual format to match capture format
                var waveFormat = device.AudioClient.MixFormat;

                // Create ring buffer wave provider for audio data
                // This reads directly from the shared ring buffer
                _ringBufferWaveProvider = new RingBufferWaveProvider(_ringBuffer!, waveFormat);

                // Create WASAPI output with low latency
                // Using 20ms latency for balanced performance
                const int latency = 20;
                _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, latency);
                _wasapiOut.Init(_ringBufferWaveProvider);
                _wasapiOut.Play();

                _isPlaying = true;
                System.Diagnostics.Debug.WriteLine($"Playback started successfully for device: {_deviceId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start playback: {ex.Message}");
                PlaybackError?.Invoke(this, ex);
                throw new InvalidOperationException($"Failed to start playback: {ex.Message}", ex);
            }
        }

        public void StopPlayback()
        {
            lock (_lockObject)
            {
                if (!_isPlaying)
                    return;

                try
                {
                    _wasapiOut?.Stop();
                    _wasapiOut?.Dispose();
                    _wasapiOut = null;

                    _ringBufferWaveProvider = null;
                    _isPlaying = false;
                    System.Diagnostics.Debug.WriteLine("Playback stopped successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping playback: {ex.Message}");
                    PlaybackError?.Invoke(this, ex);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            StopPlayback();
            _isDisposed = true;
        }
    }
}
