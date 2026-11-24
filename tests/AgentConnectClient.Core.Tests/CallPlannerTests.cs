using System.Text.Json;
using AgentConnectClient.Calling;
using AgentConnectClient.Examples;
using AgentConnectClient.Settings;

namespace AgentConnectClient.Core.Tests;

public sealed class CallPlannerTests
{
    [Fact]
    public void PrepareMcp_TwoSavedTokens_UsesTheSelectedToken()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareMcp(settings, 1, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

        Assert.True(prepared.IsReady);
        Assert.Equal("Bearer token-b", prepared.Headers["Authorization"]);
        Assert.False(prepared.Headers.ContainsKey("X-Admin-Key"));
        Assert.Equal("Ben", prepared.TokenDescription);
        Assert.True(prepared.SendsBearerToken);
        Assert.False(prepared.SendsAdminKey);
    }

    [Theory]
    [InlineData("POST", "/tenants")]
    [InlineData("POST", "/tenants/{id}/send-invitation")]
    [InlineData("POST", "/tenants/{id}/suspend")]
    [InlineData("POST", "/tenants/{id}/resume")]
    [InlineData("DELETE", "/tenants/{id}")]
    public void PrepareRest_PlatformOperator_SendsAdminKeyAndNoBearer(string method, string path)
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareRest(settings, 0, method, path, ExampleJson.ForRestEndpoint(method, path));

        Assert.True(prepared.IsReady);
        Assert.Equal("admin-key", prepared.Headers["X-Admin-Key"]);
        Assert.False(prepared.Headers.ContainsKey("Authorization"));
        Assert.True(prepared.SendsAdminKey);
        Assert.False(prepared.SendsBearerToken);
        Assert.Null(prepared.TokenDescription);
    }

    [Fact]
    public void PrepareRest_MemberCall_SendsBearerAndNoAdminKey()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/tenant", ExampleJson.ForRestEndpoint("GET", "/tenant"));

        Assert.True(prepared.IsReady);
        Assert.Equal("Bearer token-a", prepared.Headers["Authorization"]);
        Assert.False(prepared.Headers.ContainsKey("X-Admin-Key"));
        Assert.Equal("Ann", prepared.TokenDescription);
    }

    [Fact]
    public void PrepareMcp_NoTokenSelected_ReturnsClientError()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareMcp(settings, null, "list_workspaces", ExampleJson.ForMcpTool("list_workspaces"));

        Assert.False(prepared.IsReady);
        Assert.Equal(ClientErrorText.NoTokenSelected, prepared.ClientError);
    }

    [Fact]
    public void PrepareRest_AdminKeyMissing_ReturnsClientError()
    {
        var settings = SampleSettings.Create() with { AdminKey = "" };
        var prepared = CallPlanner.PrepareRest(settings, 0, "POST", "/tenants", ExampleJson.ForRestEndpoint("POST", "/tenants"));

        Assert.Equal(ClientErrorText.MissingAdminKey, prepared.ClientError);
    }

    [Fact]
    public void PrepareMcp_UrlMissing_ReturnsClientErrorBeforeJsonIsChecked()
    {
        var settings = SampleSettings.Create() with { McpServerUrl = "" };
        var prepared = CallPlanner.PrepareMcp(settings, 0, "list_workspaces", "not-json");

        Assert.Equal(ClientErrorText.MissingMcpUrl, prepared.ClientError);
    }

    [Fact]
    public void PrepareRest_UrlMissing_ReturnsClientError()
    {
        var settings = SampleSettings.Create() with { RestBaseUrl = "" };
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/tenant", "{}");

        Assert.Equal(ClientErrorText.MissingRestUrl, prepared.ClientError);
    }

    [Fact]
    public void PrepareMcp_EditorIsNotJson_ReturnsClientError()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareMcp(settings, 0, "list_workspaces", "not-json");

        Assert.Equal(ClientErrorText.InvalidJson, prepared.ClientError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[1]")]
    [InlineData("\"text\"")]
    public void PrepareRest_EditorIsNotAJsonObject_ReturnsClientError(string editorJson)
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/tenant", editorJson);

        Assert.Equal(ClientErrorText.InvalidJson, prepared.ClientError);
    }

    [Fact]
    public void PrepareRest_NoTokenAndInvalidJson_ReportsTheMissingToken()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareRest(settings, null, "GET", "/tenant", "not-json");

        Assert.Equal(ClientErrorText.NoTokenSelected, prepared.ClientError);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"path\":null}")]
    [InlineData("{\"path\":{\"id\":null}}")]
    [InlineData("{\"path\":{\"id\":\"\"}}")]
    [InlineData("{\"path\":{\"listId\":\"00000000-0000-0000-0000-000000000000\"}}")]
    public void PrepareRest_PathParameterMissing_ReturnsClientError(string editorJson)
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/workspaces/{id}/lists", editorJson);

        Assert.Equal(ClientErrorText.MissingPathParameter, prepared.ClientError);
    }

    [Fact]
    public void Compose_TrailingSlash_DoesNotProduceADoubleSlash()
    {
        var settings = SampleSettings.Create() with { RestBaseUrl = "https://example.com/api/" };
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/tenant", "{}");

        Assert.Equal("https://example.com/api/tenant", prepared.Url);
    }

    [Fact]
    public void Compose_WithoutTrailingSlash_InsertsOneSlash()
    {
        var settings = SampleSettings.Create() with { RestBaseUrl = "https://example.com/api" };
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/tenant", "{}");

        Assert.Equal("https://example.com/api/tenant", prepared.Url);
    }

    [Fact]
    public void Compose_GetItemsExample_OmitsNullQueryParameters()
    {
        var settings = SampleSettings.Create() with { RestBaseUrl = "https://example.com/" };
        var prepared = CallPlanner.PrepareRest(
            settings,
            0,
            "GET",
            "/workspaces/{id}/lists/{listId}/items",
            ExampleJson.ForRestEndpoint("GET", "/workspaces/{id}/lists/{listId}/items"));

        Assert.Equal(
            "https://example.com/workspaces/00000000-0000-0000-0000-000000000000/lists/00000000-0000-0000-0000-000000000000/items?shape=full",
            prepared.Url);
        Assert.DoesNotContain("pageSize", prepared.Url, StringComparison.Ordinal);
        Assert.DoesNotContain("continuation", prepared.Url, StringComparison.Ordinal);
        Assert.DoesNotContain("query=", prepared.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_EmailPathAndObjectQuery_AreEscaped()
    {
        var settings = SampleSettings.Create() with { RestBaseUrl = "https://example.com" };
        const string editor = """
            {
              "path": { "email": "user@example.com" },
              "query": { "pageSize": 20, "continuation": null, "query": { "belowCurrent": true } }
            }
            """;
        var prepared = CallPlanner.PrepareRest(settings, 0, "GET", "/members", """{"query":{"pageSize":20,"continuation":null}}""");
        Assert.Equal("https://example.com/members?pageSize=20", prepared.Url);

        var encoded = CallPlanner.PrepareRest(
            settings,
            0,
            "DELETE",
            "/members/{email}",
            """{"path":{"email":"user@example.com"}}""");
        Assert.Equal("https://example.com/members/user%40example.com", encoded.Url);

        var items = CallPlanner.PrepareRest(
            settings,
            0,
            "GET",
            "/workspaces/{id}/lists/{listId}/items",
            editor.Replace("\"path\": { \"email\": \"user@example.com\" }", "\"path\": { \"id\": \"w\", \"listId\": \"l\" }", StringComparison.Ordinal));
        Assert.Contains("pageSize=20", items.Url, StringComparison.Ordinal);
        Assert.DoesNotContain("continuation", items.Url, StringComparison.Ordinal);
        var filter = QueryValue(items.Url, "query");
        using var filterJson = JsonDocument.Parse(filter);
        Assert.True(filterJson.RootElement.GetProperty("belowCurrent").GetBoolean());
    }

    #region private

    private static string QueryValue(string url, string name)
    {
        var query = new Uri(url).Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = part.Split('=', 2);
            if (string.Equals(Uri.UnescapeDataString(split[0]), name, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(split[1]);
            }
        }

        throw new InvalidOperationException("The query parameter was not sent.");
    }

    #endregion

    [Fact]
    public void PrepareRest_NullBody_IsNotSent()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareRest(settings, 0, "POST", "/members", """{"body":null}""");

        Assert.True(prepared.IsReady);
        Assert.Null(prepared.Body);
    }

    [Fact]
    public void PrepareMcp_NullArgument_IsPreserved()
    {
        var settings = SampleSettings.Create();
        var prepared = CallPlanner.PrepareMcp(settings, 0, "update_workspace", """{"workspaceId":"w","description":null}""");

        Assert.True(prepared.IsReady);
        Assert.NotNull(prepared.McpArguments);
        Assert.Equal(JsonValueKind.Null, prepared.McpArguments["description"].ValueKind);
        Assert.Equal("w", prepared.McpArguments["workspaceId"].GetString());
    }

    [Fact]
    public void Prepare_SpecificationExamples_AreReadyForEveryCall()
    {
        var settings = SampleSettings.Create();
        foreach (var tool in AgentConnectClient.Catalog.McpToolCatalog.All)
        {
            var prepared = CallPlanner.PrepareMcp(settings, 0, tool.Name, ExampleJson.ForMcpTool(tool.Name));
            Assert.True(prepared.IsReady, tool.Name);
        }

        foreach (var endpoint in AgentConnectClient.Catalog.RestEndpointCatalog.All)
        {
            var prepared = CallPlanner.PrepareRest(
                settings,
                endpoint.IsPlatformOperator ? null : 0,
                endpoint.Method,
                endpoint.Path,
                ExampleJson.ForRestEndpoint(endpoint.Method, endpoint.Path));
            Assert.True(prepared.IsReady, endpoint.Display);
        }
    }
}
