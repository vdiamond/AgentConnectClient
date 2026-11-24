namespace AgentConnectClient.Settings;

/// <summary>
/// Locates the plain-text settings file inside a Windows user profile.
/// </summary>
public static class SettingsLocations
{
    /// <summary>
    /// Gets the folder name created under the user profile.
    /// </summary>
    public const string FolderName = "AgentConnectClient";

    /// <summary>
    /// Gets the settings file name.
    /// </summary>
    public const string FileName = "settings.json";

    /// <summary>
    /// Gets the settings file path for the current user profile.
    /// </summary>
    public static string DefaultFilePath => GetFilePathForProfile(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>
    /// Gets the settings file path for a profile directory.
    /// </summary>
    /// <param name="userProfilePath">The user profile directory.</param>
    /// <returns>The full settings file path.</returns>
    public static string GetFilePathForProfile(string userProfilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userProfilePath);
        return Path.Combine(userProfilePath, FolderName, FileName);
    }
}
