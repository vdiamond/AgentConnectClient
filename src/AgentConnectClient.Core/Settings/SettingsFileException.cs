namespace AgentConnectClient.Settings;

/// <summary>
/// The settings file exists but could not be read as settings.
/// </summary>
public sealed class SettingsFileException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsFileException"/> class.
    /// </summary>
    /// <param name="message">The message shown to the user. It does not include file contents.</param>
    /// <param name="filePath">The settings file path.</param>
    public SettingsFileException(string message, string filePath)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public string FilePath { get; }
}
