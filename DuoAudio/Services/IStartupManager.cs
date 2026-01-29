namespace DuoAudio.Services;

/// <summary>
/// Interface for managing Windows startup folder
/// </summary>
public interface IStartupManager
{
    /// <summary>
    /// Check if application is currently in Windows startup folder
    /// </summary>
    bool IsInStartup();

    /// <summary>
    /// Add application to Windows startup folder with specified device IDs
    /// </summary>
    /// <param name="sourceDeviceId">Source device ID</param>
    /// <param name="destinationDeviceId">Destination device ID</param>
    void AddToStartup(string sourceDeviceId, string destinationDeviceId);

    /// <summary>
    /// Remove application from Windows startup folder
    /// </summary>
    void RemoveFromStartup();
}
