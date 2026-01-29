using System;
using System.Threading;

namespace DuoAudio.Services;

/// <summary>
/// Implementation for managing single instance application behavior
/// </summary>
public class SingleInstanceManager : ISingleInstanceManager
{
    private const string MutexName = "DuoAudio_SingleInstance_Mutex";
    private const string EventName = "DuoAudio_ShowWindow";
    private Mutex? _mutex;

    /// <summary>
    /// Gets whether this is the first instance of the application
    /// </summary>
    public bool IsFirstInstance { get; private set; }

    /// <summary>
    /// Initialize the single instance manager
    /// </summary>
    public void Initialize()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            IsFirstInstance = createdNew;

            System.Diagnostics.Debug.WriteLine($"Single instance check: IsFirstInstance={IsFirstInstance}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing single instance manager: {ex.Message}");
            // If we can't create the mutex, assume we're the first instance
            IsFirstInstance = true;
        }
    }

    /// <summary>
    /// Signal the existing instance to show its window
    /// </summary>
    public void SignalExistingInstance()
    {
        try
        {
            using var eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            eventHandle.Set();
            System.Diagnostics.Debug.WriteLine("Signaled existing instance to show window");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error signaling existing instance: {ex.Message}");
        }
    }

    /// <summary>
    /// Wait for signal from another instance
    /// </summary>
    public void WaitForShowSignal(Action onShow)
    {
        try
        {
            var eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            // Wait for signal in a background thread
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    eventHandle.WaitOne();
                    onShow?.Invoke();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in show signal handler: {ex.Message}");
                }
                finally
                {
                    eventHandle.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error waiting for show signal: {ex.Message}");
        }
    }
}
