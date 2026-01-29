namespace DuoAudio.Models
{
    public class AudioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsInput { get; set; }
        public bool IsOutput { get; set; }
        public bool IsDefault { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
