namespace DuoAudio.Services
{
    public interface IAudioPlaybackService
    {
        void Initialize(string deviceId);
        void Initialize(string deviceId, int bufferConfig);
        void StartPlayback();
        void StopPlayback();
        void QueueAudioBuffer(byte[] buffer);
        bool IsPlaying { get; }
        event EventHandler<Exception>? PlaybackError;
        event EventHandler? DeviceDisconnected;
    }
}
