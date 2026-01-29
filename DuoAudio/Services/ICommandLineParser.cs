namespace DuoAudio.Services;

/// <summary>
/// Configuration parsed from command line arguments
/// </summary>
public class CommandLineConfig
{
    /// <summary>
    /// Source device ID
    /// </summary>
    public string? SourceDeviceId { get; set; }

    /// <summary>
    /// Destination device ID
    /// </summary>
    public string? DestinationDeviceId { get; set; }
}

/// <summary>
/// Interface for parsing command line arguments
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// Parse command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Parsed configuration</returns>
    CommandLineConfig Parse(string[] args);
}
