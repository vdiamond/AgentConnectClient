using System.Text.Json;
using AgentConnectClient.Catalog;
using AgentConnectClient.Settings;

namespace AgentConnectClient.Calling;

/// <summary>
/// Checks a call against the client rules and builds the request that would be sent.
/// </summary>
public static class CallPlanner
{
    /// <summary>
    /// Prepares an MCP tool call. The checks follow the order listed in FR-13.
    /// </summary>
    /// <param name="settings">The saved settings.</param>
    /// <param name="selectedTokenIndex">The selected saved token, or <see langword="null"/> when none is selected.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="editorJson">The editor text.</param>
    /// <returns>A ready call, or a client error.</returns>
    public static PreparedCall PrepareMcp(ClientSettings settings, int? selectedTokenIndex, string toolName, string editorJson)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(editorJson);
        if (McpToolCatalog.Find(toolName) is null)
        {
            throw new ArgumentException($"Unknown MCP tool '{toolName}'.", nameof(toolName));
        }

        if (settings.McpServerUrl.Length == 0)
        {
            return PreparedCall.Reject(ClientErrorText.MissingMcpUrl);
        }

        var token = settings.GetSelectedToken(selectedTokenIndex);
        if (token is null)
        {
            return PreparedCall.Reject(ClientErrorText.NoTokenSelected);
        }

        if (!TryReadObject(editorJson, out var root, out var document))
        {
            return PreparedCall.Reject(ClientErrorText.InvalidJson);
        }

        using (document)
        {
            var arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                arguments[property.Name] = property.Value.Clone();
            }

            var headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Authorization"] = "Bearer " + token.Token,
            };
            return PreparedCall.Ready(
                settings.McpServerUrl,
                "POST",
                headers,
                body: null,
                arguments,
                token.Description,
                sendsBearerToken: true,
                sendsAdminKey: false);
        }
    }

    /// <summary>
    /// Prepares a REST call. The checks follow the order listed in FR-13.
    /// </summary>
    /// <param name="settings">The saved settings.</param>
    /// <param name="selectedTokenIndex">The selected saved token, or <see langword="null"/> when none is selected.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path template.</param>
    /// <param name="editorJson">The editor text.</param>
    /// <returns>A ready call, or a client error.</returns>
    public static PreparedCall PrepareRest(
        ClientSettings settings,
        int? selectedTokenIndex,
        string method,
        string path,
        string editorJson)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(editorJson);
        var endpoint = RestEndpointCatalog.Find(method, path)
            ?? throw new ArgumentException($"Unknown REST endpoint '{method} {path}'.", nameof(path));

        if (settings.RestBaseUrl.Length == 0)
        {
            return PreparedCall.Reject(ClientErrorText.MissingRestUrl);
        }

        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        string? tokenDescription = null;
        var sendsBearer = false;
        if (endpoint.IsPlatformOperator)
        {
            if (settings.AdminKey.Length == 0)
            {
                return PreparedCall.Reject(ClientErrorText.MissingAdminKey);
            }

            headers["X-Admin-Key"] = settings.AdminKey;
        }
        else
        {
            var token = settings.GetSelectedToken(selectedTokenIndex);
            if (token is null)
            {
                return PreparedCall.Reject(ClientErrorText.NoTokenSelected);
            }

            headers["Authorization"] = "Bearer " + token.Token;
            tokenDescription = token.Description;
            sendsBearer = true;
        }

        if (!TryReadObject(editorJson, out var root, out var document))
        {
            return PreparedCall.Reject(ClientErrorText.InvalidJson);
        }

        using (document)
        {
            if (!TryReadPath(endpoint, root, out var pathValues))
            {
                return PreparedCall.Reject(ClientErrorText.MissingPathParameter);
            }

            var query = ReadQuery(root);
            var body = ReadBody(root);
            var url = RestUrlComposer.Compose(settings.RestBaseUrl, endpoint.Path, pathValues, query);
            return PreparedCall.Ready(
                url,
                endpoint.Method,
                headers,
                body,
                mcpArguments: null,
                tokenDescription,
                sendsBearer,
                sendsAdminKey: endpoint.IsPlatformOperator);
        }
    }

    #region private

    private static bool TryReadObject(string editorJson, out JsonElement root, out JsonDocument? document)
    {
        try
        {
            document = JsonDocument.Parse(editorJson);
        }
        catch (JsonException)
        {
            root = default;
            document = null;
            return false;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            document = null;
            root = default;
            return false;
        }

        root = document.RootElement;
        return true;
    }

    private static bool TryReadPath(
        RestEndpointDefinition endpoint,
        JsonElement root,
        out Dictionary<string, string> pathValues)
    {
        pathValues = new Dictionary<string, string>(StringComparer.Ordinal);
        if (endpoint.PathParameters.Count == 0)
        {
            return true;
        }

        if (!root.TryGetProperty("path", out var pathElement) || pathElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var parameter in endpoint.PathParameters)
        {
            if (!TryReadPathValue(pathElement, parameter.Name, out var value))
            {
                return false;
            }

            pathValues[parameter.Name] = value;
        }

        return true;
    }

    private static bool TryReadPathValue(JsonElement pathElement, string name, out string value)
    {
        JsonElement match = default;
        var found = false;
        foreach (var property in pathElement.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.Ordinal))
            {
                match = property.Value;
                found = true;
            }
        }

        if (!found)
        {
            value = "";
            return false;
        }

        switch (match.ValueKind)
        {
            case JsonValueKind.String:
                var text = match.GetString() ?? "";
                if (text.Length == 0)
                {
                    value = "";
                    return false;
                }

                value = text;
                return true;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                value = match.GetRawText();
                return value.Length > 0;
            default:
                value = "";
                return false;
        }
    }

    private static List<KeyValuePair<string, string>> ReadQuery(JsonElement root)
    {
        var query = new List<KeyValuePair<string, string>>();
        if (!root.TryGetProperty("query", out var queryElement) || queryElement.ValueKind != JsonValueKind.Object)
        {
            return query;
        }

        foreach (var property in queryElement.EnumerateObject())
        {
            var value = ReadQueryValue(property.Value);
            if (value is null)
            {
                continue;
            }

            query.Add(new KeyValuePair<string, string>(property.Name, value));
        }

        return query;
    }

    private static string? ReadQueryValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.String:
                return element.GetString() ?? "";
            default:
                return element.GetRawText();
        }
    }

    private static string? ReadBody(JsonElement root)
    {
        if (!root.TryGetProperty("body", out var body) || body.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return body.GetRawText();
    }

    #endregion
}
