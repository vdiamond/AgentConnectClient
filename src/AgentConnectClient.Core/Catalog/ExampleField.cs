namespace AgentConnectClient.Catalog;

/// <summary>
/// One named placeholder in a REST path, query, or body.
/// </summary>
public sealed record ExampleField
{
    /// <summary>
    /// Gets the JSON name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the placeholder written into the example JSON.
    /// </summary>
    public required ExamplePlaceholder Placeholder { get; init; }
}
