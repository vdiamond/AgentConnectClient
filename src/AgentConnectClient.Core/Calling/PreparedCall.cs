using System.Text.Json;

namespace AgentConnectClient.Calling;

/// <summary>
/// A call that is ready to send, or a client error that must not be sent.
/// </summary>
public sealed class PreparedCall
{
    private PreparedCall()
    {
    }

    /// <summary>
    /// Gets the client error, or <see langword="null"/> when the call can be sent.
    /// </summary>
    public string? ClientError { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the call can be sent.
    /// </summary>
    public bool IsReady => ClientError is null;

    /// <summary>
    /// Gets the MCP server URL or the final REST URL.
    /// </summary>
    public string Url { get; private init; } = "";

    /// <summary>
    /// Gets the HTTP method. MCP calls use POST.
    /// </summary>
    public string Method { get; private init; } = "";

    /// <summary>
    /// Gets the credential headers. This does not include <c>Content-Type</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; private init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the REST body, or <see langword="null"/> when the call has no body.
    /// </summary>
    public string? Body { get; private init; }

    /// <summary>
    /// Gets the MCP tool arguments, including JSON nulls the user left in the editor.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? McpArguments { get; private init; }

    /// <summary>
    /// Gets the description of the token that will be sent, when a Member token is used.
    /// </summary>
    public string? TokenDescription { get; private init; }

    /// <summary>
    /// Gets a value indicating whether <c>Authorization: Bearer</c> will be sent.
    /// </summary>
    public bool SendsBearerToken { get; private init; }

    /// <summary>
    /// Gets a value indicating whether <c>X-Admin-Key</c> will be sent.
    /// </summary>
    public bool SendsAdminKey { get; private init; }

    /// <summary>
    /// Creates a preparation that must not be sent.
    /// </summary>
    /// <param name="clientError">The section 9 client error text.</param>
    /// <returns>The rejected preparation.</returns>
    public static PreparedCall Reject(string clientError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientError);
        return new PreparedCall
        {
            ClientError = clientError,
        };
    }

    /// <summary>
    /// Creates a preparation that can be sent.
    /// </summary>
    /// <param name="url">The URL to call.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="headers">The credential headers.</param>
    /// <param name="body">The REST body, if any.</param>
    /// <param name="mcpArguments">The MCP arguments, if any.</param>
    /// <param name="tokenDescription">The description of the token that will be sent.</param>
    /// <param name="sendsBearerToken">Whether a Member token is sent.</param>
    /// <param name="sendsAdminKey">Whether the admin key is sent.</param>
    /// <returns>The ready preparation.</returns>
    public static PreparedCall Ready(
        string url,
        string method,
        IReadOnlyDictionary<string, string> headers,
        string? body,
        IReadOnlyDictionary<string, JsonElement>? mcpArguments,
        string? tokenDescription,
        bool sendsBearerToken,
        bool sendsAdminKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(headers);
        return new PreparedCall
        {
            Url = url,
            Method = method,
            Headers = headers,
            Body = body,
            McpArguments = mcpArguments,
            TokenDescription = tokenDescription,
            SendsBearerToken = sendsBearerToken,
            SendsAdminKey = sendsAdminKey,
        };
    }
}
