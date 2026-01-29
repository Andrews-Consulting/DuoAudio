using System;

namespace DuoAudio.Services;

/// <summary>
/// Interface for managing single instance application behavior
/// </summary>
public interface ISingleInstanceManager
{
    /// <summary>
    /// Gets whether this is the first instance of the application
    /// </summary>
    bool IsFirstInstance { get; }

    /// <summary>
    /// Initialize the single instance manager
    /// </summary>
    void Initialize();

    /// <summary>
    /// Signal the existing instance to show its window
    /// </summary>
    void SignalExistingInstance();

    /// <summary>
    /// Wait for signal from another instance
    /// </summary>
    /// <param name="onShow">Action to perform when signal is received</param>
    void WaitForShowSignal(Action onShow);
}
