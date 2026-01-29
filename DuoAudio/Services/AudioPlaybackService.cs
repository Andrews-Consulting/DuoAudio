using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DuoAudio.Services
{
    public class AudioPlaybackService : IAudioPlaybackService, IDisposable
    {
        private WasapiOut? _wasapiOut;
        private BufferedWaveProvider? _bufferedWaveProvider;
        private string? _deviceId;
        private bool _isPlaying;
        private readonly object _lockObject = new();
        private bool _isDisposed;
        private int _bufferConfig = 3; // Default to balanced

        public bool IsPlaying => _isPlaying;

        public event EventHandler<Exception>? PlaybackError;
#pragma warning disable CS0067 // Event is never used - required by interface
        public event EventHandler? DeviceDisconnected;
#pragma warning restore CS0067

        public void Initialize(string deviceId)
        {
            Initialize(deviceId, 3); // Default to balanced
        }

        public void Initialize(string deviceId, int bufferConfig)
        {
            _deviceId = deviceId;
            _bufferConfig = bufferConfig;
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
                
                // Create buffered wave provider for audio data
                // Buffer duration based on configuration
                var bufferDuration = GetBufferDurationForConfig(_bufferConfig);
                _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferDuration = bufferDuration,
                    DiscardOnBufferOverflow = true
                };

                // Create WASAPI output
                // Latency based on configuration
                var latency = GetLatencyForConfig(_bufferConfig);
                _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, latency);
                _wasapiOut.Init(_bufferedWaveProvider);
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

                    _bufferedWaveProvider = null;
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

        public void QueueAudioBuffer(byte[] buffer)
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_isPlaying || _bufferedWaveProvider == null)
                        return;

                    // Safety check: prevent null or empty buffers
                    if (buffer == null || buffer.Length == 0)
                        return;

                    // Safety check: prevent buffer overflow
                    const int maxBufferSize = 1024 * 1024; // 1MB max
                    if (buffer.Length > maxBufferSize)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Buffer size ({buffer.Length}) exceeds maximum ({maxBufferSize})");
                        return;
                    }

                    try
                    {
                        _bufferedWaveProvider.AddSamples(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        // Buffer might be full or other issue
                        System.Diagnostics.Debug.WriteLine($"Error adding samples: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in QueueAudioBuffer (outer): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            StopPlayback();
            _isDisposed = true;
        }

        private TimeSpan GetBufferDurationForConfig(int config)
        {
            return config switch
            {
                1 => TimeSpan.FromMilliseconds(10),  // Low Latency
                2 => TimeSpan.FromMilliseconds(20),  // Low-Medium
                3 => TimeSpan.FromMilliseconds(50),  // Balanced (default)
                4 => TimeSpan.FromMilliseconds(100), // Medium-High
                5 => TimeSpan.FromMilliseconds(200), // High Stability
                _ => TimeSpan.FromMilliseconds(50)   // Default to balanced
            };
        }

        private int GetLatencyForConfig(int config)
        {
            return config switch
            {
                1 => 10,  // Low Latency
                2 => 20,  // Low-Medium
                3 => 20,  // Balanced (default)
                4 => 50,  // Medium-High
                5 => 100, // High Stability
                _ => 20   // Default to balanced
            };
        }
    }
}
