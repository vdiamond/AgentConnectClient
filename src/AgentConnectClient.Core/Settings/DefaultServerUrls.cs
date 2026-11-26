namespace AgentConnectClient.Settings;

/// <summary>
/// Built-in AgentConnect prototype URLs used when the user has not saved URLs yet.
/// </summary>
public static class DefaultServerUrls
{
    /// <summary>
    /// Gets the MCP Streamable HTTP URL of the Azure prototype.
    /// </summary>
    public const string McpServerUrl = "https://ac-mcp.greenpond-2de37f99.eastus.azurecontainerapps.io/mcp";

    /// <summary>
    /// Gets the REST base URL of the Azure prototype.
    /// </summary>
    public const string RestBaseUrl = "https://ac-api.greenpond-2de37f99.eastus.azurecontainerapps.io";
}
