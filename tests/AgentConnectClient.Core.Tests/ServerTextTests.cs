using System.Net;
using System.Text;
using System.Text.Json;
using AgentConnectClient.Calling;
using AgentConnectClient.Settings;

namespace AgentConnectClient.Core.Tests;

public sealed class ServerTextTests
{
    private const string Token = "ab+cd/ef=ghZoë";

    [Fact]
    public void Pretty_PlusSlashEqualsApostropheAndNonAscii_StayInTheText()
    {
        const string body = "{\"token\":\"ab+cd/ef=ghZoë\",\"name\":\"O'Brien\"}";
        var pretty = JsonText.FormatForDisplay(200, body);

        Assert.Contains(Token, pretty, StringComparison.Ordinal);
        Assert.Contains("O'Brien", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u002B", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u002F", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u003D", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0027", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u00EB", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u00eb", pretty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunRest_TokenWithPlusSlashEqualsAndNonAscii_IsShownAndSavedUnchanged()
    {
        var rest = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token\":\"" + Token + "\"}", Encoding.UTF8, "application/json"),
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var directory = Path.Combine(Path.GetTempPath(), "agentconnectclient-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var entry = await service.RunRestAsync(SampleSettings.Create(), 0, "GET", "/tenant", "{}");
            Assert.Contains(Token, entry.Result, StringComparison.Ordinal);
            Assert.DoesNotContain("\\u00", entry.Result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\\u002B", entry.Result, StringComparison.Ordinal);

            var json = entry.Result[(entry.Result.IndexOf('\n') + 1)..];
            using var document = JsonDocument.Parse(json);
            var copied = document.RootElement.GetProperty("token").GetString();
            Assert.Equal(Token, copied);

            var store = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(directory));
            var settings = SampleSettings.Create().AddSavedToken(copied!, "Zoë");
            await store.SaveAsync(settings);
            var file = await File.ReadAllTextAsync(store.FilePath);
            Assert.Contains(Token, file, StringComparison.Ordinal);
            Assert.Contains("Zoë", file, StringComparison.Ordinal);
            Assert.DoesNotContain("\\u002B", file, StringComparison.Ordinal);
            Assert.DoesNotContain("\\u00EB", file, StringComparison.OrdinalIgnoreCase);

            var loaded = await store.LoadAsync();
            Assert.Equal(Token, loaded.SavedTokens[^1].Token);
            Assert.Equal("Zoë", loaded.SavedTokens[^1].Description);

            var prepared = CallPlanner.PrepareMcp(loaded, loaded.SavedTokens.Count - 1, "list_workspaces", "{}");
            Assert.Equal("Bearer " + Token, prepared.Headers["Authorization"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
