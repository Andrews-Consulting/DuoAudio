namespace AudioDuplication.Services
{
    public interface IAudioCaptureService
    {
        void Initialize(string deviceId);
        void StartCapture();
        void StopCapture();
        byte[]? GetAudioBuffer();
        bool IsCapturing { get; }
        event EventHandler<byte[]>? DataAvailable;
    }
}
