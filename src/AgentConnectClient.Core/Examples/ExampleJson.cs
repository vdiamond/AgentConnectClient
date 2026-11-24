using System.Text.Json;
using System.Text.Json.Nodes;
using AgentConnectClient.Catalog;

namespace AgentConnectClient.Examples;

/// <summary>
/// Builds the editable example JSON for a selected MCP tool or REST endpoint.
/// </summary>
public static class ExampleJson
{
    /// <summary>
    /// Builds the example argument object for an MCP tool.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>Pretty-printed JSON.</returns>
    public static string ForMcpTool(string toolName)
    {
        var tool = McpToolCatalog.Find(toolName)
            ?? throw new ArgumentException($"Unknown MCP tool '{toolName}'.", nameof(toolName));
        var root = new JsonObject();
        foreach (var argument in tool.Arguments)
        {
            root[argument.Name] = CreateNode(argument.Placeholder);
        }

        return Serialize(root);
    }

    /// <summary>
    /// Builds the example request object for a REST endpoint.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path template.</param>
    /// <returns>Pretty-printed JSON.</returns>
    public static string ForRestEndpoint(string method, string path)
    {
        var endpoint = RestEndpointCatalog.Find(method, path)
            ?? throw new ArgumentException($"Unknown REST endpoint '{method} {path}'.", nameof(path));
        var root = new JsonObject();
        if (endpoint.PathParameters.Count > 0)
        {
            root["path"] = CreateObject(endpoint.PathParameters);
        }

        if (endpoint.QueryParameters.Count > 0)
        {
            root["query"] = CreateObject(endpoint.QueryParameters);
        }

        if (endpoint.BodyKind == RestBodyKind.Raw)
        {
            root["body"] = new JsonObject();
        }
        else if (endpoint.BodyKind == RestBodyKind.Fields)
        {
            root["body"] = CreateObject(endpoint.BodyFields);
        }

        return Serialize(root);
    }

    #region private

    private const string GuidText = "00000000-0000-0000-0000-000000000000";

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
    };

    private static JsonObject CreateObject(IReadOnlyList<ExampleField> fields)
    {
        var obj = new JsonObject();
        foreach (var field in fields)
        {
            obj[field.Name] = CreateNode(field.Placeholder);
        }

        return obj;
    }

    private static JsonNode? CreateNode(ExamplePlaceholder placeholder)
    {
        return placeholder switch
        {
            ExamplePlaceholder.Null => null,
            ExamplePlaceholder.Guid => GuidText,
            ExamplePlaceholder.Email => "user@example.com",
            ExamplePlaceholder.OwnerEmail => "owner@example.com",
            ExamplePlaceholder.TenantName => "Example Tenant",
            ExamplePlaceholder.WorkspaceName => "Example Workspace",
            ExamplePlaceholder.ListName => "Example List",
            ExamplePlaceholder.Description => "Example description",
            ExamplePlaceholder.Role => "WorkspaceRead",
            ExamplePlaceholder.ShapeFull => "full",
            ExamplePlaceholder.True => true,
            ExamplePlaceholder.EmptyObject => new JsonObject(),
            _ => throw new ArgumentOutOfRangeException(nameof(placeholder), placeholder, "Unknown example placeholder."),
        };
    }

    private static string Serialize(JsonObject obj)
    {
        return obj.ToJsonString(Indented);
    }

    #endregion
}
