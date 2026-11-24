using AgentConnectClient.Calling;

namespace AgentConnectClient.Transport;

/// <summary>
/// What a caller observed from AgentConnect, or the reason the call was not completed.
/// </summary>
public sealed class TransportResult
{
    private TransportResult()
    {
    }

    /// <summary>
    /// Gets a value indicating whether a response was received.
    /// </summary>
    public bool ReachedServer { get; private init; }

    /// <summary>
    /// Gets the client error when the call was not sent or the server could not be reached.
    /// </summary>
    public string? ClientError { get; private init; }

    /// <summary>
    /// Gets the HTTP status code when the call has one.
    /// </summary>
    public int? StatusCode { get; private init; }

    /// <summary>
    /// Gets the raw body or tool text.
    /// </summary>
    public string Body { get; private init; } = "";

    /// <summary>
    /// Gets a value indicating whether the call succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the error text when the call did not succeed.
    /// </summary>
    public string? ErrorText { get; private init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Body"/> is server text that must be shown unchanged.
    /// </summary>
    public bool IsServerResult { get; private init; }

    /// <summary>
    /// Creates the result used when the server cannot be reached.
    /// </summary>
    /// <returns>The unreachable result.</returns>
    public static TransportResult Unreachable()
    {
        return new TransportResult
        {
            ReachedServer = false,
            ClientError = ClientErrorText.UnreachableServer,
            Body = ClientErrorText.UnreachableServer,
            Succeeded = false,
            ErrorText = ClientErrorText.UnreachableServer,
        };
    }

    /// <summary>
    /// Creates a result from an HTTP status and body. Status 200 is success, matching AgentConnect v8.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="body">The raw body.</param>
    /// <returns>The HTTP result.</returns>
    public static TransportResult FromHttp(int statusCode, string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var error = JsonText.ReadErrorProperty(body);
        var succeeded = statusCode == 200;
        return new TransportResult
        {
            ReachedServer = true,
            StatusCode = statusCode,
            Body = body,
            Succeeded = succeeded,
            ErrorText = succeeded ? null : error ?? body,
            IsServerResult = true,
        };
    }

    /// <summary>
    /// Creates a result from an MCP tool result or tool error.
    /// </summary>
    /// <param name="toolText">The tool text.</param>
    /// <param name="isError">Whether the tool reported an error.</param>
    /// <returns>The tool result.</returns>
    public static TransportResult FromTool(string toolText, bool isError)
    {
        ArgumentNullException.ThrowIfNull(toolText);
        return new TransportResult
        {
            ReachedServer = true,
            Body = toolText,
            Succeeded = !isError,
            ErrorText = isError ? toolText : null,
            IsServerResult = true,
        };
    }

    /// <summary>
    /// Creates a result from an MCP protocol failure after the server was reached.
    /// </summary>
    /// <param name="message">The protocol message.</param>
    /// <returns>The protocol result.</returns>
    public static TransportResult FromProtocol(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new TransportResult
        {
            ReachedServer = true,
            Body = message,
            Succeeded = false,
            ErrorText = message,
        };
    }
}
