namespace AgentConnectClient.Catalog;

/// <summary>
/// How a REST call carries a request body.
/// </summary>
public enum RestBodyKind
{
    /// <summary>
    /// The call has no request body.
    /// </summary>
    None,

    /// <summary>
    /// The body is a JSON object of named fields.
    /// </summary>
    Fields,

    /// <summary>
    /// The body is the schema or Item JSON itself, not wrapped.
    /// </summary>
    Raw,
}
