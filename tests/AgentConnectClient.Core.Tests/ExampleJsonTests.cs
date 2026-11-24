using System.Text.Json;
using AgentConnectClient.Catalog;
using AgentConnectClient.Examples;

namespace AgentConnectClient.Core.Tests;

public sealed class ExampleJsonTests
{
    [Fact]
    public void ForMcpTool_SetTenantOwner_MatchesSpecificationExample()
    {
        var json = ExampleJson.ForMcpTool("set_tenant_owner");
        Assert.Equal(
            """
            {
              "email": "user@example.com",
              "isTenantOwner": true
            }
            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            json);
    }

    [Fact]
    public void ForRestEndpoint_PostTenants_MatchesSpecificationExample()
    {
        var json = ExampleJson.ForRestEndpoint("POST", "/tenants");
        Assert.Equal(
            """
            {
              "body": {
                "name": "Example Tenant",
                "email": "owner@example.com"
              }
            }
            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            json);
    }

    [Fact]
    public void ForRestEndpoint_GetItems_MatchesSpecificationExample()
    {
        var json = ExampleJson.ForRestEndpoint("GET", "/workspaces/{id}/lists/{listId}/items");
        Assert.Equal(
            """
            {
              "path": {
                "id": "00000000-0000-0000-0000-000000000000",
                "listId": "00000000-0000-0000-0000-000000000000"
              },
              "query": {
                "shape": "full",
                "pageSize": null,
                "continuation": null,
                "query": null
              }
            }
            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            json);
    }

    [Fact]
    public void ForRestEndpoint_PutSchema_BodyIsTheSchemaObject()
    {
        using var document = JsonDocument.Parse(ExampleJson.ForRestEndpoint("PUT", "/workspaces/{id}/lists/{listId}/schema"));
        var root = document.RootElement;
        Assert.Equal(2, root.EnumerateObject().Count());
        Assert.Equal("00000000-0000-0000-0000-000000000000", root.GetProperty("path").GetProperty("id").GetString());
        Assert.Equal("00000000-0000-0000-0000-000000000000", root.GetProperty("path").GetProperty("listId").GetString());
        var body = root.GetProperty("body");
        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.Empty(body.EnumerateObject());
        Assert.False(body.TryGetProperty("schema", out _));
        Assert.False(root.TryGetProperty("query", out _));
    }

    [Fact]
    public void ForRestEndpoint_PutItem_BodyIsTheItemObject()
    {
        using var document = JsonDocument.Parse(ExampleJson.ForRestEndpoint("PUT", "/workspaces/{id}/lists/{listId}/items/{itemId}"));
        var body = document.RootElement.GetProperty("body");
        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.Empty(body.EnumerateObject());
        Assert.False(body.TryGetProperty("data", out _));
    }

    [Fact]
    public void ForMcpTool_EveryTool_IncludesEveryDefinedArgument()
    {
        foreach (var tool in McpToolCatalog.All)
        {
            using var document = JsonDocument.Parse(ExampleJson.ForMcpTool(tool.Name));
            var root = document.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            var names = root.EnumerateObject().Select(property => property.Name).ToArray();
            Assert.Equal(tool.Arguments.Select(argument => argument.Name).ToArray(), names);
            foreach (var argument in tool.Arguments)
            {
                var value = root.GetProperty(argument.Name);
                if (argument.Placeholder == ExamplePlaceholder.Null)
                {
                    Assert.Equal(JsonValueKind.Null, value.ValueKind);
                    Assert.False(argument.IsRequired);
                }
                else if (argument.IsRequired)
                {
                    Assert.NotEqual(JsonValueKind.Null, value.ValueKind);
                }
            }
        }
    }

    [Fact]
    public void ForMcpTool_UpdateNameAndDescription_ArePlaceholderStrings()
    {
        AssertNameAndDescriptionAreStrings(ExampleJson.ForMcpTool("update_workspace"), "Example Workspace");
        AssertNameAndDescriptionAreStrings(ExampleJson.ForMcpTool("update_list"), "Example List");
    }

    [Fact]
    public void ForRestEndpoint_PatchNameAndDescription_ArePlaceholderStrings()
    {
        using var workspace = JsonDocument.Parse(ExampleJson.ForRestEndpoint("PATCH", "/workspaces/{id}"));
        AssertNameAndDescriptionAreStrings(workspace.RootElement.GetProperty("body").GetRawText(), "Example Workspace");
        using var list = JsonDocument.Parse(ExampleJson.ForRestEndpoint("PATCH", "/workspaces/{id}/lists/{listId}"));
        AssertNameAndDescriptionAreStrings(list.RootElement.GetProperty("body").GetRawText(), "Example List");
    }

    [Fact]
    public void ForRestEndpoint_EveryEndpoint_HasOnlyThePartsThatCallUses()
    {
        foreach (var endpoint in RestEndpointCatalog.All)
        {
            using var document = JsonDocument.Parse(ExampleJson.ForRestEndpoint(endpoint.Method, endpoint.Path));
            var root = document.RootElement;
            Assert.Equal(endpoint.PathParameters.Count > 0, root.TryGetProperty("path", out var path));
            Assert.Equal(endpoint.QueryParameters.Count > 0, root.TryGetProperty("query", out var query));
            var expectsBody = endpoint.BodyKind != RestBodyKind.None;
            Assert.Equal(expectsBody, root.TryGetProperty("body", out var body));
            if (endpoint.PathParameters.Count > 0)
            {
                Assert.Equal(endpoint.PathParameters.Select(field => field.Name).ToArray(), path.EnumerateObject().Select(property => property.Name).ToArray());
            }

            if (endpoint.QueryParameters.Count > 0)
            {
                Assert.Equal(endpoint.QueryParameters.Select(field => field.Name).ToArray(), query.EnumerateObject().Select(property => property.Name).ToArray());
                foreach (var field in endpoint.QueryParameters)
                {
                    if (field.Placeholder == ExamplePlaceholder.Null)
                    {
                        Assert.Equal(JsonValueKind.Null, query.GetProperty(field.Name).ValueKind);
                    }
                }
            }

            if (endpoint.BodyKind == RestBodyKind.Fields)
            {
                Assert.Equal(endpoint.BodyFields.Select(field => field.Name).ToArray(), body.EnumerateObject().Select(property => property.Name).ToArray());
            }
        }
    }

    [Fact]
    public void ForRestEndpoint_GetTenant_IsAnEmptyObject()
    {
        Assert.Equal("{}", ExampleJson.ForRestEndpoint("GET", "/tenant"));
    }

    #region private

    private static void AssertNameAndDescriptionAreStrings(string json, string expectedName)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("name").ValueKind);
        Assert.Equal(expectedName, root.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.String, root.GetProperty("description").ValueKind);
        Assert.Equal("Example description", root.GetProperty("description").GetString());
    }

    #endregion
}
