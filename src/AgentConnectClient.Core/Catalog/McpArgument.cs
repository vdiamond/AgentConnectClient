namespace AgentConnectClient.Catalog;

/// <summary>
/// One argument of an AgentConnect MCP tool.
/// </summary>
public sealed record McpArgument
{
    /// <summary>
    /// Gets the argument name from AgentConnect v8 section 6.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the placeholder written into the example JSON.
    /// </summary>
    public required ExamplePlaceholder Placeholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether the argument is required.
    /// </summary>
    public required bool IsRequired { get; init; }
}
