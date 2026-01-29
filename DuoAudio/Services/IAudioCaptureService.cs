namespace DuoAudio.Services
{
    public interface IAudioCaptureService
    {
        void Initialize(string deviceId);
        void Initialize(string deviceId, int bufferConfig);
        void StartCapture();
        void StopCapture();
        byte[]? GetAudioBuffer();
        bool IsCapturing { get; }
        event EventHandler<byte[]>? DataAvailable;
        event EventHandler<Exception>? CaptureError;
        event EventHandler? DeviceDisconnected;
    }
}
