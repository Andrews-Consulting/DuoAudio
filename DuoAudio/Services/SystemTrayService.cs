using System;

namespace DuoAudio.Services;

/// <summary>
/// Implementation of system tray icon management
/// </summary>
public class SystemTrayService : ISystemTrayService, IDisposable
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private Action? _onIconClick;

    /// <summary>
    /// Initialize the system tray service with a NotifyIcon
    /// </summary>
    public void Initialize(System.Windows.Forms.NotifyIcon icon)
    {
        _notifyIcon = icon ?? throw new ArgumentNullException(nameof(icon));
        _notifyIcon.Click += (s, e) => _onIconClick?.Invoke();
    }

    /// <summary>
    /// Set the action to perform when the tray icon is clicked
    /// </summary>
    public void SetOnClickAction(Action onClick)
    {
        _onIconClick = onClick;
    }

    /// <summary>
    /// Show the system tray icon
    /// </summary>
    public void Show()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
        }
    }

    /// <summary>
    /// Hide the system tray icon
    /// </summary>
    public void Hide()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }

    /// <summary>
    /// Dispose of the system tray service
    /// </summary>
    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        _onIconClick = null;
    }
}
