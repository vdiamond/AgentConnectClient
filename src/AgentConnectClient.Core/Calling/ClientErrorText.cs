namespace AgentConnectClient.Calling;

/// <summary>
/// The client error copy from AgentConnectClient v1 section 9.
/// </summary>
public static class ClientErrorText
{
    /// <summary>
    /// The MCP server URL is empty.
    /// </summary>
    public const string MissingMcpUrl = "MCP server URL is not set";

    /// <summary>
    /// The REST base URL is empty.
    /// </summary>
    public const string MissingRestUrl = "REST base URL is not set";

    /// <summary>
    /// No saved token is selected for a call that needs one.
    /// </summary>
    public const string NoTokenSelected = "A token is not selected";

    /// <summary>
    /// The admin key is empty for a platform-operator call.
    /// </summary>
    public const string MissingAdminKey = "Admin key is not set";

    /// <summary>
    /// The editor text is not a JSON object.
    /// </summary>
    public const string InvalidJson = "Request is not valid JSON";

    /// <summary>
    /// A required path parameter is missing, null, or empty.
    /// </summary>
    public const string MissingPathParameter = "A path parameter is missing";

    /// <summary>
    /// The server could not be reached. This is not an AgentConnect error text.
    /// </summary>
    public const string UnreachableServer = "Could not reach the server";
}
