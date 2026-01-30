namespace DuoAudio.Services
{
    public interface IAudioPlaybackService
    {
        void Initialize(string deviceId, AudioRingBuffer ringBuffer);
        void StartPlayback();
        void StopPlayback();
        bool IsPlaying { get; }
        event EventHandler<Exception>? PlaybackError;
        event EventHandler? DeviceDisconnected;
    }
}
