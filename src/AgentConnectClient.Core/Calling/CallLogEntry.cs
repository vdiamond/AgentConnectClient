using System.Globalization;

namespace AgentConnectClient.Calling;

/// <summary>
/// One line of the in-memory call log for the current session.
/// </summary>
public sealed record CallLogEntry
{
    /// <summary>
    /// Gets the local time of the run.
    /// </summary>
    public required DateTime Time { get; init; }

    /// <summary>
    /// Gets the surface that was called.
    /// </summary>
    public required CallSurface Surface { get; init; }

    /// <summary>
    /// Gets the tool name, or the method and path template.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the HTTP status code when there is one; otherwise empty.
    /// </summary>
    public string Status { get; init; } = "";

    /// <summary>
    /// Gets the request that was sent: the final URL for a REST call, which credential kind was sent, and the editor JSON.
    /// Secret values are not included here. The editor JSON is still redacted again when the log is shown.
    /// </summary>
    public required string Request { get; init; }

    /// <summary>
    /// Gets the raw result shown in the result pane. Stored secrets are not added to it.
    /// </summary>
    public required string Result { get; init; }

    /// <summary>
    /// Gets the description of the saved token that was sent, when one was used.
    /// </summary>
    public string? TokenDescription { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client did not send the call, or could not reach the server.
    /// </summary>
    public required bool ClientError { get; init; }

    /// <summary>
    /// Gets a value indicating whether the call succeeded.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Gets the error text when the call did not succeed.
    /// </summary>
    public string? ErrorText { get; init; }

    /// <summary>
    /// Formats the single log line. Stored secrets are removed.
    /// </summary>
    /// <param name="secrets">The stored admin key and token values.</param>
    /// <returns>The log line.</returns>
    public string ToDisplayLine(IEnumerable<string> secrets)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        var time = Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var surface = Surface == CallSurface.Mcp ? "MCP" : "REST";
        var outcome = Succeeded ? "succeeded" : ErrorText ?? Result;
        var status = Status.Length == 0 ? "" : " " + Status;
        var token = string.IsNullOrEmpty(TokenDescription) ? "" : " token: " + TokenDescription;
        var line = time + " " + surface + " " + Name + status + " " + outcome + token;
        return SecretRedactor.Redact(line, secrets);
    }

    /// <summary>
    /// Formats the request and result detail for the call log. Stored secrets are removed.
    /// </summary>
    /// <param name="secrets">The stored admin key and token values.</param>
    /// <returns>The redacted request and result.</returns>
    public string ToLogDetail(IEnumerable<string> secrets)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        return SecretRedactor.Redact(Request + "\n\n" + Result, secrets);
    }
}
