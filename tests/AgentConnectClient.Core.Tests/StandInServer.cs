using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AgentConnectClient.Core.Tests;

internal sealed class StandInServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly object _gate = new();
    private readonly List<ObservedRequest> _requests = [];
    private readonly Task _loop;

    public StandInServer(Func<ObservedRequest, StandInResponse> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        BaseUrl = "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture);
        _listener.Prefixes.Add(BaseUrl + "/");
        _listener.Start();
        _loop = Task.Run(() => ListenAsync(handler));
    }

    public string BaseUrl { get; }

    public IReadOnlyList<ObservedRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return _requests.ToArray();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        try
        {
            await _loop.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }

        _listener.Close();
    }

    #region private

    private async Task ListenAsync(Func<ObservedRequest, StandInResponse> handler)
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (!_listener.IsListening)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            try
            {
                await RespondAsync(context, handler).ConfigureAwait(false);
            }
            catch (Exception)
            {
                try
                {
                    context.Response.Abort();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private async Task RespondAsync(HttpListenerContext context, Func<ObservedRequest, StandInResponse> handler)
    {
        string body;
        var encoding = context.Request.ContentEncoding ?? Encoding.UTF8;
        using (var reader = new StreamReader(context.Request.InputStream, encoding))
        {
            body = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? key in context.Request.Headers)
        {
            if (key is null)
            {
                continue;
            }

            headers[key] = context.Request.Headers[key] ?? "";
        }

        var observed = new ObservedRequest(
            context.Request.HttpMethod,
            context.Request.Url?.AbsolutePath ?? "",
            context.Request.RawUrl ?? "",
            body,
            headers,
            context.Request.ContentType);
        lock (_gate)
        {
            _requests.Add(observed);
        }

        var response = handler(observed);
        var bytes = Encoding.UTF8.GetBytes(response.Body);
        context.Response.StatusCode = response.StatusCode;
        if (response.Headers is not null)
        {
            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value;
            }
        }

        if (response.ContentType.Length > 0)
        {
            context.Response.ContentType = response.ContentType;
        }

        context.Response.ContentLength64 = bytes.Length;
        if (bytes.Length > 0)
        {
            await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        }

        context.Response.Close();
    }

    #endregion
}

internal sealed record ObservedRequest(
    string HttpMethod,
    string Path,
    string RawUrl,
    string Body,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType);

internal sealed record StandInResponse(
    int StatusCode,
    string Body,
    string ContentType = "application/json",
    IReadOnlyDictionary<string, string>? Headers = null);

internal static class McpStandIn
{
    private const string DiscoverResult = """
        {"supportedVersions":["2026-07-28"],"capabilities":{"tools":{"listChanged":false}},"serverInfo":{"name":"stand-in","version":"1.0.0"}}
        """;

    private const string InitializeResult = """
        {"protocolVersion":"2025-11-25","capabilities":{"tools":{"listChanged":false}},"serverInfo":{"name":"stand-in","version":"1.0.0"}}
        """;

    public static StandInResponse Handle(ObservedRequest request, Func<string, StandInResponse> onTool)
    {
        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return new StandInResponse(200, "", "");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(request.Body);
        }
        catch (JsonException)
        {
            return Error(400, "$: malformed JSON");
        }

        using (document)
        {
            var root = document.RootElement;
            var method = root.TryGetProperty("method", out var methodElement) ? methodElement.GetString() : null;
            if (method is null)
            {
                return Error(400, "$: malformed JSON");
            }

            if (!root.TryGetProperty("id", out var id) || method.StartsWith("notifications/", StringComparison.Ordinal))
            {
                return new StandInResponse(202, "", "");
            }

            var idText = id.GetRawText();
            if (string.Equals(method, "server/discover", StringComparison.Ordinal))
            {
                return Json(Rpc(idText, DiscoverResult.Trim()));
            }

            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                return Json(Rpc(idText, InitializeResult.Trim()));
            }

            if (string.Equals(method, "tools/call", StringComparison.Ordinal))
            {
                var name = root.GetProperty("params").GetProperty("name").GetString() ?? "";
                var tool = onTool(name);
                if (tool.StatusCode != 200)
                {
                    return tool;
                }

                return Json(Rpc(idText, tool.Body));
            }

            return Json(Rpc(idText, ToolBody("", isError: false)));
        }
    }

    public static StandInResponse ToolText(string text, bool isError)
    {
        return Json(ToolBody(text, isError));
    }

    public static StandInResponse Error(int statusCode, string errorText)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["error"] = errorText,
        });
        return new StandInResponse(statusCode, body);
    }

    #region private

    private static StandInResponse Json(string body)
    {
        return new StandInResponse(200, body);
    }

    private static string Rpc(string idText, string resultJson)
    {
        return "{\"jsonrpc\":\"2.0\",\"id\":" + idText + ",\"result\":" + resultJson + "}";
    }

    private static string ToolBody(string text, bool isError)
    {
        return "{\"content\":[{\"type\":\"text\",\"text\":" + JsonSerializer.Serialize(text) + "}],\"isError\":" + (isError ? "true" : "false") + "}";
    }

    #endregion
}
