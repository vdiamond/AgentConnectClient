using System.Text.Json;
using AgentConnectClient.Calling;
using AgentConnectClient.Examples;
using AgentConnectClient.Settings;

namespace AgentConnectClient.Core.Tests;

public sealed class StandInServerTests
{
    [Fact(Timeout = 20000)]
    public async Task RunMcp_ListWorkspaces_ShowsTheRawToolResult()
    {
        await using var server = new StandInServer(request => McpStandIn.Handle(
            request,
            name => McpStandIn.ToolText("{\"workspaces\":[{\"id\":\"w1\",\"name\":\"Ops\"}]}", isError: false)));
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { McpServerUrl = server.BaseUrl + "/mcp" };
        var entry = await service.RunMcpAsync(settings, 0, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));
        var second = await service.RunMcpAsync(settings, 1, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

        Assert.False(entry.ClientError);
        Assert.True(second.Succeeded);
        Assert.Contains("Ops", second.Result, StringComparison.Ordinal);
        Assert.True(entry.Succeeded);
        Assert.Contains("Ops", entry.Result, StringComparison.Ordinal);
        Assert.Equal("", entry.Status);
        var toolCalls = server.Requests.Where(request => request.Body.Contains("\"tools/call\"", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, toolCalls.Length);
        Assert.Contains(server.Requests, request => Header(request, "Authorization") == "Bearer token-a");
        Assert.Contains(server.Requests, request => Header(request, "Authorization") == "Bearer token-b");
        Assert.All(server.Requests, request => Assert.False(request.Headers.ContainsKey("X-Admin-Key")));
        Assert.Contains("succeeded", entry.ToDisplayLine(settings.SecretValues()), StringComparison.Ordinal);
        Assert.DoesNotContain("token-a", entry.ToDisplayLine(settings.SecretValues()), StringComparison.Ordinal);
        Assert.DoesNotContain("token-b", second.ToDisplayLine(settings.SecretValues()), StringComparison.Ordinal);
    }

    [Fact(Timeout = 20000)]
    public async Task RunMcp_ToolError_ShowsTheErrorTextUnchanged()
    {
        await using var server = new StandInServer(request => McpStandIn.Handle(
            request,
            _ => McpStandIn.ToolText("email: required", isError: true)));
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { McpServerUrl = server.BaseUrl + "/mcp" };
        var entry = await service.RunMcpAsync(settings, 0, "set_tenant_owner", ExampleJson.ForMcpTool("set_tenant_owner"));

        Assert.Equal("email: required", entry.Result);
        Assert.Equal("email: required", entry.ErrorText);
        Assert.False(entry.Succeeded);
        Assert.False(entry.ClientError);
        Assert.Equal("", entry.Status);
    }

    [Fact(Timeout = 20000)]
    public async Task RunMcp_HttpRejection_ShowsStatusAndBody()
    {
        await using var server = new StandInServer(_ => McpStandIn.Error(401, "$: invalid token"));
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { McpServerUrl = server.BaseUrl + "/mcp" };
        var entry = await service.RunMcpAsync(settings, 0, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

        Assert.Equal("401", entry.Status);
        Assert.Contains("$: invalid token", entry.Result, StringComparison.Ordinal);
        Assert.Equal("$: invalid token", entry.ErrorText);
        Assert.False(entry.ClientError);
        Assert.NotEmpty(server.Requests);
    }

    [Fact(Timeout = 20000)]
    public async Task RunRest_PostTenantsAndGetTenant_UseTheDocumentedCredentials()
    {
        await using var server = new StandInServer(request =>
        {
            if (string.Equals(request.Path, "/tenants", StringComparison.Ordinal))
            {
                return new StandInResponse(
                    200,
                    "{\"id\":\"t1\",\"name\":\"Example Tenant\",\"status\":\"Active\",\"pageSizeDefault\":100,\"pageSizeMax\":1000,\"firstMemberId\":\"m1\",\"emailSent\":true}");
            }

            return new StandInResponse(200, "{\"pageSizeDefault\":100,\"pageSizeMax\":1000}");
        });
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { RestBaseUrl = server.BaseUrl + "/" };
        var created = await service.RunRestAsync(settings, 0, "POST", "/tenants", ExampleJson.ForRestEndpoint("POST", "/tenants"));
        var read = await service.RunRestAsync(settings, 1, "GET", "/tenant", "{}");

        Assert.Equal("200", created.Status);
        Assert.Contains("Active", created.Result, StringComparison.Ordinal);
        Assert.Equal("admin-key", Header(server.Requests[0], "X-Admin-Key"));
        Assert.False(server.Requests[0].Headers.ContainsKey("Authorization"));
        Assert.Equal("/tenant", server.Requests[1].Path);
        Assert.DoesNotContain("//tenant", server.Requests[1].RawUrl, StringComparison.Ordinal);
        Assert.Equal("Bearer token-b", Header(server.Requests[1], "Authorization"));
        Assert.False(server.Requests[1].Headers.ContainsKey("X-Admin-Key"));
        Assert.Equal("200", read.Status);
        Assert.Equal("application/json", server.Requests[0].ContentType);
    }

    [Fact(Timeout = 20000)]
    public async Task RunRest_BadTokenAndMissingEmail_ShowDocumentedErrors()
    {
        await using var server = new StandInServer(request =>
        {
            if (request.Body.Contains("\"email\":null", StringComparison.Ordinal))
            {
                return McpStandIn.Error(400, "email: required");
            }

            return McpStandIn.Error(401, "$: invalid token");
        });
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { RestBaseUrl = server.BaseUrl };
        var rejected = await service.RunRestAsync(settings, 0, "GET", "/workspaces", ExampleJson.ForRestEndpoint("GET", "/workspaces"));
        var missing = await service.RunRestAsync(settings, 0, "POST", "/members", """{"body":{"email":null}}""");

        Assert.Equal("401", rejected.Status);
        Assert.Equal("$: invalid token", rejected.ErrorText);
        Assert.Contains("$: invalid token", rejected.Result, StringComparison.Ordinal);
        Assert.Equal("400", missing.Status);
        Assert.Equal("email: required", missing.ErrorText);
        Assert.Contains("email: required", missing.Result, StringComparison.Ordinal);
    }

    [Fact(Timeout = 20000)]
    public async Task SaveTokenFromStandInResult_ThenSelectIt_SendsThatToken()
    {
        await using var server = new StandInServer(request =>
        {
            if (request.Path.Contains("/token/regenerate", StringComparison.Ordinal))
            {
                return new StandInResponse(200, "{\"token\":\"returned-token-value\",\"lastTokenIssued\":\"2026-11-24T18:00:00Z\"}");
            }

            return new StandInResponse(200, "{\"pageSizeDefault\":100,\"pageSizeMax\":1000}");
        });
        using var service = new AgentConnectCallService();
        var directory = Path.Combine(Path.GetTempPath(), "agentconnectclient-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new FileSettingsStore(SettingsLocations.GetFilePathForProfile(directory));
            var settings = SampleSettings.Create() with { RestBaseUrl = server.BaseUrl };
            var issued = await service.RunRestAsync(
                settings,
                0,
                "POST",
                "/members/{email}/token/regenerate",
                ExampleJson.ForRestEndpoint("POST", "/members/{email}/token/regenerate"));
            using var document = JsonDocument.Parse(issued.Result[(issued.Result.IndexOf('\n') + 1)..]);
            var token = document.RootElement.GetProperty("token").GetString()!;
            settings = settings.AddSavedToken(token, "From stand-in");
            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();
            var index = -1;
            for (var i = 0; i < loaded.SavedTokens.Count; i++)
            {
                if (loaded.SavedTokens[i].Description == "From stand-in")
                {
                    index = i;
                }
            }

            var followUp = await service.RunRestAsync(loaded, index, "GET", "/tenant", "{}");
            Assert.True(index >= 0);
            Assert.Equal("Bearer returned-token-value", Header(server.Requests[^1], "Authorization"));
            Assert.Equal("From stand-in", followUp.TokenDescription);
            Assert.Contains("returned-token-value", issued.Result, StringComparison.Ordinal);
            Assert.DoesNotContain("returned-token-value", followUp.ToLogDetail(loaded.SecretValues()), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact(Timeout = 20000)]
    public async Task RunRest_Redirect_IsNotFollowedAndAdminKeyIsNotSent()
    {
        await using var other = new StandInServer(_ => new StandInResponse(200, "{\"stolen\":true}"));
        await using var server = new StandInServer(_ => RedirectTo(other.BaseUrl + "/stolen"));
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with
        {
            RestBaseUrl = server.BaseUrl,
            AdminKey = "admin-key-value",
        };
        var entry = await service.RunRestAsync(settings, 0, "POST", "/tenants", ExampleJson.ForRestEndpoint("POST", "/tenants"));

        Assert.Equal("302", entry.Status);
        Assert.Contains("moved", entry.Result, StringComparison.Ordinal);
        Assert.False(entry.ClientError);
        Assert.Empty(other.Requests);
        Assert.Equal("admin-key-value", Header(server.Requests[0], "X-Admin-Key"));
        Assert.DoesNotContain("admin-key-value", string.Join("\n", other.Requests.Select(request => request.RawUrl)), StringComparison.Ordinal);
    }

    [Fact(Timeout = 20000)]
    public async Task RunMcp_Redirect_IsNotFollowedAndTokenIsNotSent()
    {
        await using var other = new StandInServer(_ => new StandInResponse(200, "{\"stolen\":true}"));
        await using var server = new StandInServer(_ => RedirectTo(other.BaseUrl + "/stolen"));
        using var service = new AgentConnectCallService();
        var settings = SampleSettings.Create() with { McpServerUrl = server.BaseUrl + "/mcp" };
        var entry = await service.RunMcpAsync(settings, 0, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

        Assert.Equal("302", entry.Status);
        Assert.Contains("moved", entry.Result, StringComparison.Ordinal);
        Assert.False(entry.ClientError);
        Assert.Empty(other.Requests);
        Assert.Equal("Bearer token-a", Header(server.Requests[0], "Authorization"));
    }

    #region private

    private static StandInResponse RedirectTo(string location)
    {
        return new StandInResponse(
            302,
            "{\"error\":\"moved\"}",
            "application/json",
            new Dictionary<string, string>
            {
                ["Location"] = location,
            });
    }

    private static string Header(ObservedRequest request, string name)
    {
        if (!request.Headers.TryGetValue(name, out var value))
        {
            throw new InvalidOperationException("Missing header " + name + ".");
        }

        return value;
    }

    #endregion
}
