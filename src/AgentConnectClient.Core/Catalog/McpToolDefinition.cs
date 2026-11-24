namespace AgentConnectClient.Catalog;

/// <summary>
/// One AgentConnect v8 MCP tool and the arguments the client sends.
/// </summary>
public sealed record McpToolDefinition
{
    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the arguments in the order they appear in the example JSON.
    /// </summary>
    public required IReadOnlyList<McpArgument> Arguments { get; init; }
}
