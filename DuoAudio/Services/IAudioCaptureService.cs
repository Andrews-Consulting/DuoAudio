namespace DuoAudio.Services
{
    public interface IAudioCaptureService
    {
        void Initialize(string deviceId, AudioRingBuffer ringBuffer);
        void StartCapture();
        void StopCapture();
        bool IsCapturing { get; }
        event EventHandler<Exception>? CaptureError;
        event EventHandler? DeviceDisconnected;
    }
}
