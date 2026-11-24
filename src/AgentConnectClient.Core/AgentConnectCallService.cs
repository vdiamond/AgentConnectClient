using AgentConnectClient.Calling;
using AgentConnectClient.Settings;
using AgentConnectClient.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentConnectClient;

/// <summary>
/// Runs one MCP or REST call and returns the result pane text and the call-log entry.
/// </summary>
public sealed class AgentConnectCallService : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConnectCallService"/> class.
    /// </summary>
    /// <param name="mcpHandler">An HTTP handler for MCP calls. Tests use this to stand in for the server.</param>
    /// <param name="restHandler">An HTTP handler for REST calls. Tests use this to stand in for the server.</param>
    /// <param name="loggerFactory">The logger factory. Secret values are not logged.</param>
    public AgentConnectCallService(
        HttpMessageHandler? mcpHandler = null,
        HttpMessageHandler? restHandler = null,
        ILoggerFactory? loggerFactory = null)
    {
        var factory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = factory.CreateLogger<AgentConnectCallService>();
        _mcp = new McpToolCaller(mcpHandler, factory);
        _rest = new RestCaller(restHandler, factory.CreateLogger<RestCaller>());
    }

    /// <summary>
    /// Runs an MCP tool. A client error is not sent.
    /// </summary>
    /// <param name="settings">The saved settings.</param>
    /// <param name="selectedTokenIndex">The selected saved token, or <see langword="null"/> when none is selected.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="editorJson">The editor text.</param>
    /// <param name="cancellationToken">The token to cancel the call.</param>
    /// <returns>The call-log entry, including the result-pane text.</returns>
    public async Task<CallLogEntry> RunMcpAsync(
        ClientSettings settings,
        int? selectedTokenIndex,
        string toolName,
        string editorJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(editorJson);
        var prepared = CallPlanner.PrepareMcp(settings, selectedTokenIndex, toolName, editorJson);
        if (!prepared.IsReady)
        {
            return Finish(settings, CallSurface.Mcp, toolName, prepared, editorJson, transport: null);
        }

        var transport = await _mcp.CallAsync(
            prepared.Url,
            toolName,
            prepared.McpArguments ?? new Dictionary<string, System.Text.Json.JsonElement>(),
            BearerValue(prepared),
            cancellationToken).ConfigureAwait(false);
        return Finish(settings, CallSurface.Mcp, toolName, prepared, editorJson, transport);
    }

    /// <summary>
    /// Runs a REST call. A client error is not sent.
    /// </summary>
    /// <param name="settings">The saved settings.</param>
    /// <param name="selectedTokenIndex">The selected saved token, or <see langword="null"/> when none is selected.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path template.</param>
    /// <param name="editorJson">The editor text.</param>
    /// <param name="cancellationToken">The token to cancel the call.</param>
    /// <returns>The call-log entry, including the result-pane text.</returns>
    public async Task<CallLogEntry> RunRestAsync(
        ClientSettings settings,
        int? selectedTokenIndex,
        string method,
        string path,
        string editorJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(editorJson);
        var prepared = CallPlanner.PrepareRest(settings, selectedTokenIndex, method, path, editorJson);
        if (!prepared.IsReady)
        {
            return Finish(settings, CallSurface.Rest, method + " " + path, prepared, editorJson, transport: null);
        }

        var transport = await _rest.SendAsync(
            prepared.Method,
            prepared.Url,
            prepared.Headers,
            prepared.Body,
            cancellationToken).ConfigureAwait(false);
        return Finish(settings, CallSurface.Rest, method + " " + path, prepared, editorJson, transport);
    }

    /// <summary>
    /// Disposes the MCP and REST callers.
    /// </summary>
    public void Dispose()
    {
        _mcp.Dispose();
        _rest.Dispose();
    }

    #region private

    private readonly McpToolCaller _mcp;
    private readonly RestCaller _rest;
    private readonly ILogger<AgentConnectCallService> _logger;

    private CallLogEntry Finish(
        ClientSettings settings,
        CallSurface surface,
        string name,
        PreparedCall prepared,
        string editorJson,
        TransportResult? transport)
    {
        CallLogEntry entry;
        if (!prepared.IsReady)
        {
            var error = prepared.ClientError ?? ClientErrorText.InvalidJson;
            entry = new CallLogEntry
            {
                Time = DateTime.Now,
                Surface = surface,
                Name = name,
                Status = "",
                Request = editorJson,
                Result = error,
                TokenDescription = null,
                ClientError = true,
                Succeeded = false,
                ErrorText = error,
            };
        }
        else if (transport is null)
        {
            throw new ArgumentNullException(nameof(transport));
        }
        else if (transport.ClientError is not null)
        {
            entry = new CallLogEntry
            {
                Time = DateTime.Now,
                Surface = surface,
                Name = name,
                Status = "",
                Request = BuildRequest(prepared, editorJson),
                Result = transport.ClientError,
                TokenDescription = prepared.SendsBearerToken ? prepared.TokenDescription : null,
                ClientError = true,
                Succeeded = false,
                ErrorText = transport.ClientError,
            };
        }
        else
        {
            var secrets = settings.SecretValues();
            var body = transport.IsServerResult
                ? transport.Body
                : SecretRedactor.Redact(transport.Body, secrets);
            var errorText = transport.ErrorText;
            if (errorText is not null && !transport.IsServerResult)
            {
                errorText = SecretRedactor.Redact(errorText, secrets);
            }

            entry = new CallLogEntry
            {
                Time = DateTime.Now,
                Surface = surface,
                Name = name,
                Status = transport.StatusCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                Request = BuildRequest(prepared, editorJson),
                Result = JsonText.FormatForDisplay(transport.StatusCode, body),
                TokenDescription = prepared.SendsBearerToken ? prepared.TokenDescription : null,
                ClientError = false,
                Succeeded = transport.Succeeded,
                ErrorText = transport.Succeeded ? null : errorText,
            };
        }

        _logger.LogInformation(
            "Call {Surface} {Name} finished. Succeeded {Succeeded}. Status {Status}. ClientError {ClientError}.",
            entry.Surface,
            entry.Name,
            entry.Succeeded,
            entry.Status,
            entry.ClientError);
        return entry;
    }

    private static string BuildRequest(PreparedCall prepared, string editorJson)
    {
        var credential = prepared.SendsAdminKey
            ? "X-Admin-Key sent"
            : prepared.SendsBearerToken
                ? "Authorization: Bearer sent"
                : "No credential sent";
        return prepared.Url + "\n" + credential + "\n" + editorJson;
    }

    private static string BearerValue(PreparedCall prepared)
    {
        if (!prepared.Headers.TryGetValue("Authorization", out var header))
        {
            return "";
        }

        const string prefix = "Bearer ";
        if (header.StartsWith(prefix, StringComparison.Ordinal))
        {
            return header[prefix.Length..];
        }

        return header;
    }

    #endregion
}
