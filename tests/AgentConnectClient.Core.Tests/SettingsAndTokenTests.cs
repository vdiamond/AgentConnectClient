using AgentConnectClient.Calling;
using AgentConnectClient.Examples;
using AgentConnectClient.Settings;
using Microsoft.Extensions.Logging;

namespace AgentConnectClient.Core.Tests;

public sealed class SettingsAndTokenTests
{
    [Fact]
    public async Task SaveAsync_RoundTrip_PreservesUrlsAdminKeyAndTokens()
    {
        var directory = CreateProfile("same-user");
        try
        {
            var path = SettingsLocations.GetFilePathForProfile(directory);
            var store = new FileSettingsStore(path, new ListLogger<FileSettingsStore>());
            var settings = SampleSettings.Create();
            await store.SaveAsync(settings);

            var file = await File.ReadAllTextAsync(path);
            Assert.Contains("token-a", file, StringComparison.Ordinal);
            Assert.Contains("admin-key", file, StringComparison.Ordinal);
            Assert.Contains("Ann", file, StringComparison.Ordinal);
            Assert.DoesNotContain("\"tokenHash\"", file, StringComparison.Ordinal);

            var loaded = await store.LoadAsync();
            Assert.Equal(settings.McpServerUrl, loaded.McpServerUrl);
            Assert.Equal(settings.RestBaseUrl, loaded.RestBaseUrl);
            Assert.Equal(settings.AdminKey, loaded.AdminKey);
            Assert.Equal(["Ann", "Ben"], loaded.SavedTokens.Select(token => token.Description).ToArray());
            Assert.Equal(["token-a", "token-b"], loaded.SavedTokens.Select(token => token.Token).ToArray());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_DifferentProfile_DoesNotSeeTheFirstUsersSecrets()
    {
        var root = CreateProfile("root");
        var first = Path.Combine(root, "first");
        var second = Path.Combine(root, "second");
        try
        {
            var firstStore = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(first));
            await firstStore.SaveAsync(SampleSettings.Create());
            var secondStore = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(second));
            var loaded = await secondStore.LoadAsync();

            Assert.Equal("", loaded.AdminKey);
            Assert.Empty(loaded.SavedTokens);
            Assert.Equal(DefaultServerUrls.McpServerUrl, loaded.McpServerUrl);
            Assert.Equal(DefaultServerUrls.RestBaseUrl, loaded.RestBaseUrl);
            Assert.NotEqual(SampleSettings.Create().McpServerUrl, loaded.McpServerUrl);
            Assert.NotEqual(firstStore.FilePath, secondStore.FilePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DefaultFilePath_IsInsideTheCurrentUserProfile()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = SettingsLocations.DefaultFilePath;
        Assert.StartsWith(profile, path, StringComparison.Ordinal);
        Assert.EndsWith(Path.Combine("AgentConnectClient", "settings.json"), path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsBuiltInDefaultUrls()
    {
        var directory = CreateProfile("missing");
        try
        {
            var store = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(directory));
            var loaded = await store.LoadAsync();
            Assert.Equal(DefaultServerUrls.McpServerUrl, loaded.McpServerUrl);
            Assert.Equal(DefaultServerUrls.RestBaseUrl, loaded.RestBaseUrl);
            Assert.Equal("", loaded.AdminKey);
            Assert.Empty(loaded.SavedTokens);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_SavedUrls_AreNotReplacedByDefaults()
    {
        var directory = CreateProfile("saved-urls");
        try
        {
            var path = SettingsLocations.GetFilePathForProfile(directory);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(
                path,
                """
                {
                  "mcpServerUrl": "",
                  "restBaseUrl": "https://saved.example.test"
                }
                """);
            var store = new FileSettingsStore(path);
            var loaded = await store.LoadAsync();
            Assert.Equal("", loaded.McpServerUrl);
            Assert.Equal("https://saved.example.test", loaded.RestBaseUrl);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_FileWithoutUrlFields_UsesBuiltInDefaults()
    {
        var directory = CreateProfile("absent-urls");
        try
        {
            var path = SettingsLocations.GetFilePathForProfile(directory);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, """{"adminKey":"kept-key"}""");
            var store = new FileSettingsStore(path);
            var loaded = await store.LoadAsync();
            Assert.Equal(DefaultServerUrls.McpServerUrl, loaded.McpServerUrl);
            Assert.Equal(DefaultServerUrls.RestBaseUrl, loaded.RestBaseUrl);
            Assert.Equal("kept-key", loaded.AdminKey);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_InvalidFile_DoesNotIncludeFileContentsInTheMessage()
    {
        var directory = CreateProfile("corrupt");
        try
        {
            var path = SettingsLocations.GetFilePathForProfile(directory);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "{ \"adminKey\": \"super-secret-key\"");
            var logger = new ListLogger<FileSettingsStore>();
            var store = new FileSettingsStore(path, logger);

            var exception = await Assert.ThrowsAsync<SettingsFileException>(() => store.LoadAsync());

            Assert.Equal("Settings file is not valid JSON.", exception.Message);
            Assert.DoesNotContain("super-secret-key", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("super-secret-key", string.Join(" ", logger.Messages), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AddSavedToken_SaveAndSelect_UsesTheReturnedToken()
    {
        var directory = CreateProfile("returned");
        try
        {
            var store = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(directory));
            var settings = SampleSettings.Create().AddSavedToken("returned-token-value", "Returned");
            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();
            var index = IndexOf(loaded, "Returned");
            var prepared = CallPlanner.PrepareMcp(loaded, index, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

            Assert.Equal("Bearer returned-token-value", prepared.Headers["Authorization"]);
            Assert.Equal("Returned", prepared.TokenDescription);
            Assert.Equal("Bearer token-a", CallPlanner.PrepareMcp(loaded, 0, "list_workspaces", "{}").Headers["Authorization"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ToDisplayLine_StoredSecrets_AreRemovedFromTheLogAndKeptInTheResult()
    {
        var settings = SampleSettings.Create();
        var entry = new CallLogEntry
        {
            Time = new DateTime(2026, 11, 24, 13, 19, 0),
            Surface = CallSurface.Rest,
            Name = "GET /tenant",
            Status = "200",
            Request = "https://api.example.test/tenant\nAuthorization: Bearer sent\n{}",
            Result = "{\"token\":\"token-a\",\"admin\":\"admin-key\"}",
            TokenDescription = "Ann",
            ClientError = false,
            Succeeded = true,
            ErrorText = null,
        };

        var line = entry.ToDisplayLine(settings.SecretValues());
        var detail = entry.ToLogDetail(settings.SecretValues());
        Assert.Contains("succeeded", line, StringComparison.Ordinal);
        Assert.Contains("Ann", line, StringComparison.Ordinal);
        Assert.DoesNotContain("token-a", line, StringComparison.Ordinal);
        Assert.DoesNotContain("admin-key", line, StringComparison.Ordinal);
        Assert.DoesNotContain("token-a", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("admin-key", detail, StringComparison.Ordinal);
        Assert.Contains("token-a", entry.Result, StringComparison.Ordinal);
        Assert.Contains("admin-key", entry.Result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDisplayLine_OneCharacterSecrets_LeaveOrdinaryWordsReadable()
    {
        var settings = new ClientSettings
        {
            McpServerUrl = "https://mcp.example.test/mcp",
            RestBaseUrl = "https://api.example.test",
            AdminKey = "e",
            SavedTokens =
            [
                new SavedToken
                {
                    Token = "a",
                    Description = "Ann",
                },
            ],
        };
        var entry = new CallLogEntry
        {
            Time = new DateTime(2026, 11, 24, 13, 19, 0),
            Surface = CallSurface.Rest,
            Name = "GET /workspaces",
            Status = "200",
            Request = "https://api.example.test/workspaces\nAuthorization: Bearer sent\n{}",
            Result = """{"workspaces":[],"token":"a","admin":"e"}""",
            TokenDescription = "Ann",
            ClientError = false,
            Succeeded = true,
            ErrorText = null,
        };

        var line = entry.ToDisplayLine(settings.SecretValues());
        Assert.Contains("GET /workspaces", line, StringComparison.Ordinal);
        Assert.Contains("succeeded", line, StringComparison.Ordinal);
        Assert.Contains("Ann", line, StringComparison.Ordinal);
        Assert.DoesNotContain("***", line, StringComparison.Ordinal);

        var detail = entry.ToLogDetail(settings.SecretValues());
        Assert.Contains("workspaces", detail, StringComparison.Ordinal);
        Assert.Contains("Bearer sent", detail, StringComparison.Ordinal);
        Assert.Contains("example", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("\"a\"", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("\"e\"", detail, StringComparison.Ordinal);
        Assert.Contains("\"token\":\"***\"", detail, StringComparison.Ordinal);
        Assert.Contains("\"admin\":\"***\"", detail, StringComparison.Ordinal);
        Assert.Contains("\"token\":\"a\"", entry.Result, StringComparison.Ordinal);
        Assert.Contains("\"admin\":\"e\"", entry.Result, StringComparison.Ordinal);
    }

    #region private

    private static string CreateProfile(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), "agentconnectclient-tests", Guid.NewGuid().ToString("N"), name);
        Directory.CreateDirectory(path);
        return path;
    }

    private static int IndexOf(ClientSettings settings, string description)
    {
        for (var index = 0; index < settings.SavedTokens.Count; index++)
        {
            if (string.Equals(settings.SavedTokens[index].Description, description, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new InvalidOperationException("The description was not saved.");
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return EmptyDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    #endregion
}
