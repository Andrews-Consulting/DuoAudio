using AudioDuplication.Models;

namespace AudioDuplication.Services
{
    public interface IAudioDeviceEnumerator
    {
        List<AudioDeviceInfo> GetOutputDevices();
        List<AudioDeviceInfo> GetInputDevices();
        void SubscribeToDeviceChanges(Action deviceChangedCallback);
        void UnsubscribeFromDeviceChanges();
    }
}
