namespace DuoAudio.Services;

/// <summary>
/// Implementation for parsing command line arguments
/// </summary>
public class CommandLineParser : ICommandLineParser
{
    /// <summary>
    /// Parse command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Parsed configuration</returns>
    public CommandLineConfig Parse(string[] args)
    {
        var config = new CommandLineConfig();

        if (args == null || args.Length == 0)
            return config;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--source-id":
                    if (i + 1 < args.Length)
                        config.SourceDeviceId = args[++i];
                    break;
                case "--dest-id":
                    if (i + 1 < args.Length)
                        config.DestinationDeviceId = args[++i];
                    break;
            }
        }

        System.Diagnostics.Debug.WriteLine($"Parsed command line: Source={config.SourceDeviceId}, Dest={config.DestinationDeviceId}");

        return config;
    }
}
