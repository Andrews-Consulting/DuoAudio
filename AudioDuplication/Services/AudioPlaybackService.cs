using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioDuplication.Services
{
    public class AudioPlaybackService : IAudioPlaybackService, IDisposable
    {
        private WasapiOut? _wasapiOut;
        private BufferedWaveProvider? _bufferedWaveProvider;
        private string? _deviceId;
        private bool _isPlaying;
        private readonly object _lockObject = new();

        public bool IsPlaying => _isPlaying;

        public void Initialize(string deviceId)
        {
            _deviceId = deviceId;
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

                // Create buffered wave provider for audio data
                // Use standard format: 44.1kHz, 16-bit, stereo
                var waveFormat = new WaveFormat(44100, 16, 2);
                _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(1),
                    DiscardOnBufferOverflow = true
                };

                // Create WASAPI output
                _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
                _wasapiOut.Init(_bufferedWaveProvider);
                _wasapiOut.Play();

                _isPlaying = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start playback: {ex.Message}", ex);
            }
        }

        public void StopPlayback()
        {
            lock (_lockObject)
            {
                if (!_isPlaying)
                    return;

                _wasapiOut?.Stop();
                _wasapiOut?.Dispose();
                _wasapiOut = null;

                _bufferedWaveProvider = null;
                _isPlaying = false;
            }
        }

        public void QueueAudioBuffer(byte[] buffer)
        {
            lock (_lockObject)
            {
                if (!_isPlaying || _bufferedWaveProvider == null)
                    return;

                try
                {
                    _bufferedWaveProvider.AddSamples(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    // Buffer might be full or other issue
                    System.Diagnostics.Debug.WriteLine($"Error adding samples: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            StopPlayback();
        }
    }
}
