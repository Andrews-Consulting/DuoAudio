using NAudio.CoreAudioApi;
using DuoAudio.Models;

namespace DuoAudio.Services
{
    public class AudioDeviceEnumerator : IAudioDeviceEnumerator, IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator;
        private Action? _deviceChangedCallback;

        public AudioDeviceEnumerator()
        {
            _enumerator = new MMDeviceEnumerator();
            _enumerator.RegisterEndpointNotificationCallback(new DeviceNotificationCallback(this));
        }

        public List<AudioDeviceInfo> GetOutputDevices()
        {
            var devices = new List<AudioDeviceInfo>();
            var mmDevices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in mmDevices)
            {
                devices.Add(new AudioDeviceInfo
                {
                    Id = device.ID,
                    Name = device.FriendlyName,
                    IsInput = false,
                    IsOutput = true,
                    IsDefault = device.ID == GetDefaultOutputDeviceId()
                });
            }

            return devices;
        }

        public List<AudioDeviceInfo> GetInputDevices()
        {
            var devices = new List<AudioDeviceInfo>();
            var mmDevices = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            foreach (var device in mmDevices)
            {
                devices.Add(new AudioDeviceInfo
                {
                    Id = device.ID,
                    Name = device.FriendlyName,
                    IsInput = true,
                    IsOutput = false,
                    IsDefault = device.ID == GetDefaultInputDeviceId()
                });
            }

            return devices;
        }

        public void SubscribeToDeviceChanges(Action deviceChangedCallback)
        {
            _deviceChangedCallback = deviceChangedCallback;
        }

        public void UnsubscribeFromDeviceChanges()
        {
            _deviceChangedCallback = null;
        }

        private string GetDefaultOutputDeviceId()
        {
            try
            {
                var defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return defaultDevice.ID;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetDefaultInputDeviceId()
        {
            try
            {
                var defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                return defaultDevice.ID;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal void OnDeviceChanged()
        {
            _deviceChangedCallback?.Invoke();
        }

        public void Dispose()
        {
            _enumerator?.Dispose();
        }

        private class DeviceNotificationCallback : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
        {
            private readonly AudioDeviceEnumerator _parent;

            public DeviceNotificationCallback(AudioDeviceEnumerator parent)
            {
                _parent = parent;
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                _parent.OnDeviceChanged();
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                _parent.OnDeviceChanged();
            }

            public void OnDeviceRemoved(string deviceId)
            {
                _parent.OnDeviceChanged();
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                _parent.OnDeviceChanged();
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                // Not needed for basic device enumeration
            }
        }
    }
}
