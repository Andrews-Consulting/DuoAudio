namespace AudioDuplication.Services
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

        public AudioDuplicationWorker(
            IAudioCaptureService captureService,
            IAudioPlaybackService playbackService)
        {
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
            _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));

            // Wire up capture data available event
            _captureService.DataAvailable += OnCaptureDataAvailable;
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
            if (!_isRunning || audioData == null || audioData.Length == 0)
                return;

            try
            {
                // Send captured audio to playback
                _playbackService.QueueAudioBuffer(audioData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error queueing audio: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _captureService.DataAvailable -= OnCaptureDataAvailable;
            _cancellationTokenSource?.Dispose();
        }
    }
}
