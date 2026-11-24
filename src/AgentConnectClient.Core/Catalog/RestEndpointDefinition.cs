namespace AgentConnectClient.Catalog;

/// <summary>
/// One AgentConnect v8 REST endpoint.
/// </summary>
public sealed record RestEndpointDefinition
{
    /// <summary>
    /// Gets the HTTP method.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets the path template, including <c>{id}</c> style parameters.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets a value indicating whether the call uses the platform admin key.
    /// </summary>
    public required bool IsPlatformOperator { get; init; }

    /// <summary>
    /// Gets the path parameters in path order.
    /// </summary>
    public IReadOnlyList<ExampleField> PathParameters { get; init; } = [];

    /// <summary>
    /// Gets the query parameters in example order.
    /// </summary>
    public IReadOnlyList<ExampleField> QueryParameters { get; init; } = [];

    /// <summary>
    /// Gets how the request body is shaped.
    /// </summary>
    public RestBodyKind BodyKind { get; init; } = RestBodyKind.None;

    /// <summary>
    /// Gets the body fields when <see cref="BodyKind"/> is <see cref="RestBodyKind.Fields"/>.
    /// </summary>
    public IReadOnlyList<ExampleField> BodyFields { get; init; } = [];

    /// <summary>
    /// Gets the method and path as shown on the REST screen.
    /// </summary>
    public string Display => Method + " " + Path;
}
