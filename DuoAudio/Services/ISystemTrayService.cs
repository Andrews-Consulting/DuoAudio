namespace DuoAudio.Services;

/// <summary>
/// Interface for system tray icon management
/// </summary>
public interface ISystemTrayService
{
    /// <summary>
    /// Initialize the system tray service with a NotifyIcon
    /// </summary>
    void Initialize(System.Windows.Forms.NotifyIcon icon);

    /// <summary>
    /// Set the action to perform when the tray icon is clicked
    /// </summary>
    void SetOnClickAction(System.Action onClick);

    /// <summary>
    /// Show the system tray icon
    /// </summary>
    void Show();

    /// <summary>
    /// Hide the system tray icon
    /// </summary>
    void Hide();

    /// <summary>
    /// Dispose of the system tray service
    /// </summary>
    void Dispose();
}
