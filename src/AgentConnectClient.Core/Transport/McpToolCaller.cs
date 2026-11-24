using System.Text.Json;
using AgentConnectClient.Calling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AgentConnectClient.Transport;

/// <summary>
/// Sends one AgentConnect MCP tool call over Streamable HTTP and then drops the connection.
/// </summary>
public sealed class McpToolCaller : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolCaller"/> class.
    /// </summary>
    /// <param name="handler">The HTTP handler. When omitted, a new handler is owned by this instance.</param>
    /// <param name="loggerFactory">The logger factory passed to the MCP client. Secret values are not logged.</param>
    public McpToolCaller(HttpMessageHandler? handler = null, ILoggerFactory? loggerFactory = null)
    {
        if (handler is null)
        {
            // A redirect must not carry the Member token to another host.
            _handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
            };
            _ownsHandler = true;
        }
        else
        {
            _handler = handler;
            _ownsHandler = false;
        }

        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<McpToolCaller>();
    }

    /// <summary>
    /// Calls one tool. The client is not kept, and no session is retained.
    /// </summary>
    /// <param name="url">The MCP server URL.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="arguments">The tool arguments, including JSON nulls.</param>
    /// <param name="bearerToken">The selected Member token.</param>
    /// <param name="cancellationToken">The token to cancel the call.</param>
    /// <returns>The tool result, the HTTP rejection, or a client error when the server cannot be reached.</returns>
    public async Task<TransportResult> CallAsync(
        string url,
        string toolName,
        IReadOnlyDictionary<string, JsonElement> arguments,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(bearerToken);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            return TransportResult.Unreachable();
        }

        var capture = new ResponseCaptureHandler(_handler);
        using var httpClient = new HttpClient(capture, disposeHandler: false)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        // Stateless Streamable HTTP: one connection per tool call, no standalone event stream,
        // and no client timeout. A slow first response after the server scales to zero is waited out.
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            Name = "AgentConnectClient",
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false,
            OwnsSession = false,
            ConnectionTimeout = Timeout.InfiniteTimeSpan,
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Authorization"] = "Bearer " + bearerToken,
            },
        };

        await using var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: false);
        try
        {
            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "AgentConnectClient",
                    Version = "1.0.0",
                },
                InitializationTimeout = Timeout.InfiniteTimeSpan,
                DiscoverProbeTimeout = Timeout.InfiniteTimeSpan,
            };
            await using var client = await McpClient.CreateAsync(transport, clientOptions, _loggerFactory, cancellationToken).ConfigureAwait(false);
            var argumentDictionary = new Dictionary<string, JsonElement>(arguments.Count, StringComparer.Ordinal);
            foreach (var pair in arguments)
            {
                argumentDictionary[pair.Key] = pair.Value;
            }

            var result = await client.CallToolAsync(
                new CallToolRequestParams
                {
                    Name = toolName,
                    Arguments = argumentDictionary,
                },
                cancellationToken).ConfigureAwait(false);
            var toolResult = TransportResult.FromTool(ReadToolText(result), result.IsError == true);
            _logger.LogInformation(
                "MCP tool {Tool} completed. Succeeded {Succeeded}.",
                toolName,
                toolResult.Succeeded);
            return toolResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return FromHttpFailure(capture, ex, toolName);
        }
        catch (IOException)
        {
            return FromCapturedOrUnreachable(capture, toolName);
        }
        catch (McpException ex)
        {
            if (capture.LastNonSuccess is { } captured)
            {
                _logger.LogInformation(
                    "MCP tool {Tool} was rejected at the HTTP connection. Status {Status}.",
                    toolName,
                    captured.StatusCode);
                return TransportResult.FromHttp(captured.StatusCode, captured.Body);
            }

            var http = FindHttpRequest(ex);
            if (http?.StatusCode is { } code)
            {
                _logger.LogInformation(
                    "MCP tool {Tool} was rejected at the HTTP connection. Status {Status}.",
                    toolName,
                    (int)code);
                return TransportResult.FromHttp((int)code, ExtractBody(http.Message));
            }

            _logger.LogInformation("MCP tool {Tool} returned a protocol error.", toolName);
            return TransportResult.FromProtocol(ex.Message);
        }
    }

    /// <summary>
    /// Disposes the owned HTTP handler.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHandler)
        {
            _handler.Dispose();
        }
    }

    #region private

    private readonly HttpMessageHandler _handler;
    private readonly bool _ownsHandler;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    private TransportResult FromHttpFailure(ResponseCaptureHandler capture, HttpRequestException exception, string toolName)
    {
        if (capture.LastNonSuccess is { } captured)
        {
            _logger.LogInformation(
                "MCP tool {Tool} was rejected at the HTTP connection. Status {Status}.",
                toolName,
                captured.StatusCode);
            return TransportResult.FromHttp(captured.StatusCode, captured.Body);
        }

        if (exception.StatusCode is { } code)
        {
            _logger.LogInformation(
                "MCP tool {Tool} was rejected at the HTTP connection. Status {Status}.",
                toolName,
                (int)code);
            return TransportResult.FromHttp((int)code, ExtractBody(exception.Message));
        }

        _logger.LogInformation("MCP tool {Tool} could not reach the server.", toolName);
        return TransportResult.Unreachable();
    }

    private TransportResult FromCapturedOrUnreachable(ResponseCaptureHandler capture, string toolName)
    {
        if (capture.LastNonSuccess is { } captured)
        {
            _logger.LogInformation(
                "MCP tool {Tool} was rejected at the HTTP connection. Status {Status}.",
                toolName,
                captured.StatusCode);
            return TransportResult.FromHttp(captured.StatusCode, captured.Body);
        }

        _logger.LogInformation("MCP tool {Tool} could not reach the server.", toolName);
        return TransportResult.Unreachable();
    }

    private static HttpRequestException? FindHttpRequest(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is HttpRequestException http)
            {
                return http;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static string ExtractBody(string message)
    {
        const string marker = "Response body: ";
        var index = message.LastIndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return "";
        }

        return message[(index + marker.Length)..];
    }

    private static string ReadToolText(CallToolResult result)
    {
        var parts = new List<string>();
        if (result.Content is not null)
        {
            foreach (var block in result.Content)
            {
                if (block is TextContentBlock text && text.Text is not null)
                {
                    parts.Add(text.Text);
                }
            }
        }

        if (parts.Count == 0 && result.StructuredContent is { } structured)
        {
            return structured.GetRawText();
        }

        return string.Join("\n", parts);
    }

    #endregion
}
