namespace AgentConnectClient.Calling;

/// <summary>
/// Which AgentConnect surface a call uses.
/// </summary>
public enum CallSurface
{
    /// <summary>
    /// An MCP tool call.
    /// </summary>
    Mcp,

    /// <summary>
    /// A REST call.
    /// </summary>
    Rest,
}
