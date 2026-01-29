namespace AudioDuplication.Services
{
    public interface IAudioDuplicationWorker
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
        event EventHandler<string>? StatusChanged;
    }
}
