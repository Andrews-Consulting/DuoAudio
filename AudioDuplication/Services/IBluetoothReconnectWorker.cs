namespace AudioDuplication.Services
{
    public interface IBluetoothReconnectWorker
    {
        void StartMonitoring(string deviceId);
        void StopMonitoring();
        bool IsMonitoring { get; }
        bool IsDeviceConnected { get; }
        event EventHandler<bool>? ConnectionStatusChanged;
        event EventHandler<string>? ReconnectAttempted;
    }
}
