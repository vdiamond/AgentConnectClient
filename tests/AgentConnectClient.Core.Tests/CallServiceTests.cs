using System.Net;
using System.Text;
using AgentConnectClient.Calling;
using AgentConnectClient.Examples;
using AgentConnectClient.Settings;

namespace AgentConnectClient.Core.Tests;

public sealed class CallServiceTests
{
    [Fact]
    public async Task RunMcp_NoTokenSelected_DoesNotSend()
    {
        var mcp = new RecordingHandler();
        var rest = new RecordingHandler();
        using var service = new AgentConnectCallService(mcp, rest);
        var entry = await service.RunMcpAsync(SampleSettings.Create(), null, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

        Assert.Equal(0, mcp.Calls);
        Assert.Equal(0, rest.Calls);
        Assert.True(entry.ClientError);
        Assert.Equal(ClientErrorText.NoTokenSelected, entry.Result);
        Assert.Equal(ClientErrorText.NoTokenSelected, entry.ErrorText);
    }

    [Fact]
    public async Task RunRest_InvalidJson_DoesNotSend()
    {
        var rest = new RecordingHandler();
        using var service = new AgentConnectCallService(restHandler: rest);
        var entry = await service.RunRestAsync(SampleSettings.Create(), 0, "GET", "/tenant", "not-json");

        Assert.Equal(0, rest.Calls);
        Assert.Equal(ClientErrorText.InvalidJson, entry.Result);
        Assert.True(entry.ClientError);
    }

    [Fact]
    public async Task RunRest_AdminKeyMissing_DoesNotSend()
    {
        var rest = new RecordingHandler();
        using var service = new AgentConnectCallService(restHandler: rest);
        var settings = SampleSettings.Create() with { AdminKey = "" };
        var entry = await service.RunRestAsync(settings, 0, "POST", "/tenants", ExampleJson.ForRestEndpoint("POST", "/tenants"));

        Assert.Equal(0, rest.Calls);
        Assert.Equal(ClientErrorText.MissingAdminKey, entry.Result);
    }

    [Fact]
    public async Task RunRest_GetWorkspaces_ShowsStatusAndBody()
    {
        var rest = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"workspaces\":[]}", Encoding.UTF8, "application/json"),
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var entry = await service.RunRestAsync(SampleSettings.Create(), 1, "GET", "/workspaces", ExampleJson.ForRestEndpoint("GET", "/workspaces"));

        Assert.Equal(1, rest.Calls);
        Assert.Equal("Bearer token-b", rest.Requests[0].Authorization);
        Assert.Null(rest.Requests[0].AdminKey);
        Assert.Null(rest.Requests[0].ContentType);
        Assert.Equal("200", entry.Status);
        Assert.Contains("\"workspaces\"", entry.Result, StringComparison.Ordinal);
        Assert.StartsWith("200\n", entry.Result, StringComparison.Ordinal);
        Assert.False(entry.ClientError);
        Assert.True(entry.Succeeded);
        Assert.Contains("succeeded", entry.ToDisplayLine(SampleSettings.Create().SecretValues()), StringComparison.Ordinal);
        Assert.Contains("Ben", entry.ToDisplayLine(SampleSettings.Create().SecretValues()), StringComparison.Ordinal);
        Assert.DoesNotContain("token-b", entry.ToDisplayLine(SampleSettings.Create().SecretValues()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunRest_PostTenants_SendsAdminKeyAndNotBearer()
    {
        var rest = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"id\":\"t1\",\"name\":\"Example Tenant\",\"status\":\"Active\",\"pageSizeDefault\":100,\"pageSizeMax\":1000,\"firstMemberId\":\"m1\",\"emailSent\":true}",
                    Encoding.UTF8,
                    "application/json"),
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var entry = await service.RunRestAsync(SampleSettings.Create(), 0, "POST", "/tenants", ExampleJson.ForRestEndpoint("POST", "/tenants"));

        Assert.Equal("admin-key", rest.Requests[0].AdminKey);
        Assert.Null(rest.Requests[0].Authorization);
        Assert.Equal("application/json", rest.Requests[0].ContentType);
        Assert.Contains("Example Tenant", rest.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("owner@example.com", rest.Requests[0].Body, StringComparison.Ordinal);
        Assert.Equal("200", entry.Status);
        Assert.Contains("emailSent", entry.Result, StringComparison.Ordinal);
        Assert.Null(entry.TokenDescription);
        Assert.DoesNotContain("admin-key", entry.ToLogDetail(SampleSettings.Create().SecretValues()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunRest_InvalidToken_ShowsStatusAndErrorTextUnchanged()
    {
        var rest = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"$: invalid token\"}", Encoding.UTF8, "application/json"),
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var entry = await service.RunRestAsync(SampleSettings.Create(), 0, "GET", "/tenant", "{}");

        Assert.Equal("401", entry.Status);
        Assert.Contains("$: invalid token", entry.Result, StringComparison.Ordinal);
        Assert.Equal("$: invalid token", entry.ErrorText);
        Assert.False(entry.ClientError);
        Assert.False(entry.Succeeded);
        Assert.Contains("401", entry.ToDisplayLine([]), StringComparison.Ordinal);
        Assert.Contains("$: invalid token", entry.ToDisplayLine([]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunRest_RequiredEmailNull_ShowsDocumentedErrorUnchanged()
    {
        var rest = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"email: required\"}", Encoding.UTF8, "application/json"),
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var entry = await service.RunRestAsync(SampleSettings.Create(), 0, "POST", "/members", """{"body":{"email":null}}""");

        Assert.Equal("400", entry.Status);
        Assert.Equal("email: required", entry.ErrorText);
        Assert.Contains("email: required", entry.Result, StringComparison.Ordinal);
        Assert.Contains("\"email\":null", rest.Requests[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunRest_ServerResultContainsStoredSecrets_ResultPaneKeepsThemAndLogRemovesThem()
    {
        var rest = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token\":\"token-a\",\"echo\":\"admin-key\"}", Encoding.UTF8, "application/json"),
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var settings = SampleSettings.Create();
        var entry = await service.RunRestAsync(settings, 0, "GET", "/tenant", "{}");

        Assert.Contains("token-a", entry.Result, StringComparison.Ordinal);
        Assert.Contains("admin-key", entry.Result, StringComparison.Ordinal);
        Assert.DoesNotContain("token-a", entry.ToLogDetail(settings.SecretValues()), StringComparison.Ordinal);
        Assert.DoesNotContain("admin-key", entry.ToLogDetail(settings.SecretValues()), StringComparison.Ordinal);
        Assert.DoesNotContain("token-a", entry.Request, StringComparison.Ordinal);
        Assert.Contains("Authorization: Bearer sent", entry.Request, StringComparison.Ordinal);
    }

    [Fact(Timeout = 15000)]
    public async Task RunRest_UnreachableHost_ShowsClientErrorWithoutAnAgentConnectText()
    {
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { RestBaseUrl = "http://127.0.0.1:1" };
        var entry = await service.RunRestAsync(settings, 0, "GET", "/tenant", "{}");

        Assert.True(entry.ClientError);
        Assert.Equal(ClientErrorText.UnreachableServer, entry.Result);
        Assert.DoesNotContain("$:", entry.Result, StringComparison.Ordinal);
        Assert.Equal("", entry.Status);
    }

    [Fact]
    public async Task SaveReturnedToken_ThenMemberCall_SendsThatToken()
    {
        var rest = new RecordingHandler
        {
            Responder = request =>
            {
                if (request.Url.Contains("/members/", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"token\":\"returned-token-value\",\"lastTokenIssued\":\"2026-11-24T00:00:00Z\"}", Encoding.UTF8, "application/json"),
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"pageSizeDefault\":100,\"pageSizeMax\":1000}", Encoding.UTF8, "application/json"),
                };
            },
        };
        using var service = new AgentConnectCallService(restHandler: rest);
        var directory = Path.Combine(Path.GetTempPath(), "agentconnectclient-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(directory));
            var settings = SampleSettings.Create();
            var first = await service.RunRestAsync(
                settings,
                0,
                "POST",
                "/members/{email}/token/regenerate",
                ExampleJson.ForRestEndpoint("POST", "/members/{email}/token/regenerate"));
            Assert.Contains("returned-token-value", first.Result, StringComparison.Ordinal);

            using var result = System.Text.Json.JsonDocument.Parse(ExtractJson(first.Result));
            var returned = result.RootElement.GetProperty("token").GetString()!;
            settings = settings.AddSavedToken(returned, "Returned");
            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();
            var index = 0;
            for (var i = 0; i < loaded.SavedTokens.Count; i++)
            {
                if (loaded.SavedTokens[i].Description == "Returned")
                {
                    index = i;
                }
            }

            var second = await service.RunRestAsync(loaded, index, "GET", "/tenant", "{}");
            Assert.Equal("Bearer returned-token-value", rest.Requests[^1].Authorization);
            Assert.Equal("200", second.Status);
            Assert.Equal("Returned", second.TokenDescription);
            Assert.Contains("Returned", loaded.SavedTokens.Select(token => token.Description));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    #region private

    private static string ExtractJson(string result)
    {
        var newline = result.IndexOf('\n', StringComparison.Ordinal);
        return newline < 0 ? result : result[(newline + 1)..];
    }

    #endregion
}
