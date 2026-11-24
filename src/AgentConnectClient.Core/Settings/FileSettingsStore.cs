using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentConnectClient.Settings;

/// <summary>
/// Reads and writes the plain-text settings file for one user profile.
/// </summary>
public sealed class FileSettingsStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSettingsStore"/> class.
    /// </summary>
    /// <param name="filePath">The settings file path. When omitted, the current user profile is used.</param>
    /// <param name="logger">The logger. Secret values are not written to it.</param>
    public FileSettingsStore(string? filePath = null, ILogger<FileSettingsStore>? logger = null)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath) ? SettingsLocations.DefaultFilePath : filePath;
        _logger = logger ?? NullLogger<FileSettingsStore>.Instance;
    }

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Loads settings. A missing file returns empty settings.
    /// </summary>
    /// <param name="cancellationToken">The token to cancel the read.</param>
    /// <returns>The stored settings, or empty settings when the file does not exist yet.</returns>
    public async Task<ClientSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            _logger.LogInformation("Settings file does not exist yet.");
            return new ClientSettings();
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(FilePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Settings file could not be read. ExceptionType {ExceptionType}.", ex.GetType().Name);
            throw new SettingsFileException("Settings file could not be read.", FilePath);
        }

        try
        {
            var settings = JsonSerializer.Deserialize<ClientSettings>(json, SerializerOptions);
            if (settings is null)
            {
                throw new JsonException("Settings JSON was empty.");
            }

            var normalized = Normalize(settings);
            _logger.LogInformation("Loaded settings. TokenCount {TokenCount}.", normalized.SavedTokens.Count);
            return normalized;
        }
        catch (SettingsFileException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError("Settings file is not valid JSON. Line {Line}.", ex.LineNumber);
            throw new SettingsFileException("Settings file is not valid JSON.", FilePath);
        }
    }

    /// <summary>
    /// Writes settings as plain text. Token values and the admin key are not encrypted.
    /// </summary>
    /// <param name="settings">The settings to store.</param>
    /// <param name="cancellationToken">The token to cancel the write.</param>
    /// <returns>A task that completes when the file has been replaced.</returns>
    public async Task SaveAsync(ClientSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(Normalize(settings), SerializerOptions);
        var temporary = FilePath + ".tmp";
        await File.WriteAllTextAsync(temporary, json, Utf8NoBom, cancellationToken).ConfigureAwait(false);
        File.Move(temporary, FilePath, overwrite: true);
        _logger.LogInformation("Saved settings. TokenCount {TokenCount}.", settings.SavedTokens.Count);
    }

    #region private

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly ILogger<FileSettingsStore> _logger;

    private static ClientSettings Normalize(ClientSettings settings)
    {
        var tokens = new List<SavedToken>(settings.SavedTokens?.Count ?? 0);
        if (settings.SavedTokens is not null)
        {
            foreach (var saved in settings.SavedTokens)
            {
                tokens.Add(new SavedToken
                {
                    Token = saved.Token ?? "",
                    Description = saved.Description ?? "",
                });
            }
        }

        return new ClientSettings
        {
            McpServerUrl = settings.McpServerUrl ?? "",
            RestBaseUrl = settings.RestBaseUrl ?? "",
            AdminKey = settings.AdminKey ?? "",
            SavedTokens = tokens,
        };
    }

    #endregion
}
