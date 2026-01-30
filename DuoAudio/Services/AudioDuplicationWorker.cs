namespace DuoAudio.Services
{
    public class AudioDuplicationWorker : IAudioDuplicationWorker, IDisposable
    {
        private readonly IAudioCaptureService _captureService;
        private readonly IAudioPlaybackService _playbackService;
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

                    // Start capture - data flows directly to ring buffer
                    _captureService.StartCapture();

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

                IsRunning = false;
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
        }
    }
}
