namespace AudioDuplication.Services
{
    public interface IAudioPlaybackService
    {
        void Initialize(string deviceId);
        void StartPlayback();
        void StopPlayback();
        void QueueAudioBuffer(byte[] buffer);
        bool IsPlaying { get; }
    }
}
