namespace DuoAudio.Services
{
    public class AudioDuplicationWorker : IAudioDuplicationWorker, IDisposable
    {
        private readonly IAudioCaptureService _captureService;
        private readonly IAudioPlaybackService _playbackService;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _duplicationTask;
        private bool _isRunning;
        private readonly object _lockObject = new();

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                StatusChanged?.Invoke(this, _isRunning ? "Active" : "Idle");
            }
        }

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<Exception>? ErrorOccurred;

        public AudioDuplicationWorker(
            IAudioCaptureService captureService,
            IAudioPlaybackService playbackService)
        {
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
            _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
            
            // Wire up error and disconnection events (these can be wired early)
            _captureService.CaptureError += OnCaptureError;
            _captureService.DeviceDisconnected += OnDeviceDisconnected;
            _playbackService.PlaybackError += OnPlaybackError;
            _playbackService.DeviceDisconnected += OnDeviceDisconnected;
        }

        public void Start()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                    return;

                try
                {
                    // Start playback first (so it's ready to receive data)
                    _playbackService.StartPlayback();

                    // Wire up capture data available event AFTER playback is started
                    _captureService.DataAvailable += OnCaptureDataAvailable;

                    // Start capture
                    _captureService.StartCapture();

                    // Create cancellation token for the background task
                    _cancellationTokenSource = new CancellationTokenSource();

                    // Start the duplication task
                    _duplicationTask = Task.Run(() => RunDuplicationLoop(_cancellationTokenSource.Token));

                    IsRunning = true;
                }
                catch (Exception ex)
                {
                    // Clean up if start fails
                    Stop();
                    throw new InvalidOperationException($"Failed to start duplication: {ex.Message}", ex);
                }
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning)
                    return;

                // Signal cancellation
                _cancellationTokenSource?.Cancel();

                // Unwire capture data available event BEFORE stopping services
                _captureService.DataAvailable -= OnCaptureDataAvailable;

                // Stop capture and playback
                try
                {
                    _captureService.StopCapture();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping capture: {ex.Message}");
                }

                try
                {
                    _playbackService.StopPlayback();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping playback: {ex.Message}");
                }

                // Wait for the duplication task to complete (with timeout)
                if (_duplicationTask != null)
                {
                    try
                    {
                        _duplicationTask.Wait(TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error waiting for duplication task: {ex.Message}");
                    }
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _duplicationTask = null;

                IsRunning = false;
            }
        }

        private void RunDuplicationLoop(CancellationToken cancellationToken)
        {
            try
            {
                StatusChanged?.Invoke(this, "Active");

                // The actual duplication happens via the DataAvailable event
                // This loop just keeps the task alive and checks for cancellation
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Small delay to prevent tight loop
                    Task.Delay(100, cancellationToken).Wait(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Error: {ex.Message}");
            }
        }

        private void OnCaptureDataAvailable(object? sender, byte[] audioData)
        {
            try
            {
                if (!_isRunning || audioData == null || audioData.Length == 0)
                    return;

                // Safety check: prevent buffer overflow
                const int maxBufferSize = 1024 * 1024; // 1MB max
                if (audioData.Length > maxBufferSize)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Audio data size ({audioData.Length}) exceeds maximum ({maxBufferSize})");
                    return;
                }

                // Additional safety check: verify playback service is ready
                if (_playbackService == null)
                {
                    System.Diagnostics.Debug.WriteLine("Error: Playback service is null");
                    return;
                }

                if (!_playbackService.IsPlaying)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Playback service is not playing, skipping audio data");
                    return;
                }

                // Send captured audio to playback
                _playbackService.QueueAudioBuffer(audioData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnCaptureDataAvailable: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OnCaptureError(object? sender, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            ErrorOccurred?.Invoke(this, ex);
            
            // If it's a device disconnection, stop gracefully
            if (IsDeviceDisconnectionError(ex))
            {
                System.Diagnostics.Debug.WriteLine("Device disconnected detected in capture service");
                Stop();
            }
        }

        private void OnPlaybackError(object? sender, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
            ErrorOccurred?.Invoke(this, ex);
            
            // If it's a device disconnection, stop gracefully
            if (IsDeviceDisconnectionError(ex))
            {
                System.Diagnostics.Debug.WriteLine("Device disconnected detected in playback service");
                Stop();
            }
        }

        private void OnDeviceDisconnected(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Device disconnected event received");
            StatusChanged?.Invoke(this, "Error: Device disconnected");
            Stop();
        }

        private bool IsDeviceDisconnectionError(Exception ex)
        {
            if (ex == null) return false;
            
            var errorMessage = ex.Message.ToLower();
            return errorMessage.Contains("device") && 
                   (errorMessage.Contains("disconnected") || 
                    errorMessage.Contains("not available") ||
                    errorMessage.Contains("not present") ||
                    errorMessage.Contains("invalid"));
        }

        public void Dispose()
        {
            Stop();
            _captureService.CaptureError -= OnCaptureError;
            _captureService.DeviceDisconnected -= OnDeviceDisconnected;
            _playbackService.PlaybackError -= OnPlaybackError;
            _playbackService.DeviceDisconnected -= OnDeviceDisconnected;
            _cancellationTokenSource?.Dispose();
        }
    }
}
