using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DuoAudio.Services;

/// <summary>
/// Implementation for managing Windows startup folder
/// </summary>
public class StartupManager : IStartupManager
{
    private const string ShortcutName = "DuoAudio.lnk";

    /// <summary>
    /// Check if application is currently in Windows startup folder
    /// </summary>
    public bool IsInStartup()
    {
        var shortcutPath = GetShortcutPath();
        return File.Exists(shortcutPath);
    }

    /// <summary>
    /// Add application to Windows startup folder with specified device IDs
    /// </summary>
    /// <param name="sourceDeviceId">Source device ID</param>
    /// <param name="destinationDeviceId">Destination device ID</param>
    public void AddToStartup(string sourceDeviceId, string destinationDeviceId)
    {
        if (string.IsNullOrEmpty(sourceDeviceId))
            throw new ArgumentException("Source device ID cannot be null or empty", nameof(sourceDeviceId));

        if (string.IsNullOrEmpty(destinationDeviceId))
            throw new ArgumentException("Destination device ID cannot be null or empty", nameof(destinationDeviceId));

        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                throw new InvalidOperationException("Could not determine executable path");

            var shortcutPath = GetShortcutPath();
            var arguments = $"--source-id \"{sourceDeviceId}\" --dest-id \"{destinationDeviceId}\"";

            // Create shortcut using WScript.Shell with dynamic/late binding
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                throw new InvalidOperationException("Could not create WScript.Shell type");
            dynamic shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("Could not create WScript.Shell instance");
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.Arguments = arguments;
            shortcut.Description = "DuoAudio Application";
            shortcut.Save();

            System.Diagnostics.Debug.WriteLine($"Added to startup: {shortcutPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding to startup: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Remove application from Windows startup folder
    /// </summary>
    public void RemoveFromStartup()
    {
        try
        {
            var shortcutPath = GetShortcutPath();
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
                System.Diagnostics.Debug.WriteLine($"Removed from startup: {shortcutPath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing from startup: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get the full path to the startup shortcut
    /// </summary>
    private string GetShortcutPath()
    {
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(startupFolder, ShortcutName);
    }

    #region COM Interop for WScript.Shell

    [ComImport]
    [Guid("F935DC20-1CF0-11D0-ADB9-00C04FD58A0B")]
    private class WshShell { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("F935DC21-1CF0-11D0-ADB9-00C04FD58A0B")]
    private interface IWshShortcut
    {
        string TargetPath { get; set; }
        string Arguments { get; set; }
        string Description { get; set; }
        string WorkingDirectory { get; set; }
        string IconLocation { get; set; }
        void Save();
    }

    #endregion
}
