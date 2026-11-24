using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentConnectClient.Transport;

/// <summary>
/// Sends one AgentConnect REST call.
/// </summary>
public sealed class RestCaller : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestCaller"/> class.
    /// </summary>
    /// <param name="handler">The HTTP handler. When omitted, a new handler is owned by this instance.</param>
    /// <param name="logger">The logger. Secret values are not written to it.</param>
    public RestCaller(HttpMessageHandler? handler = null, ILogger<RestCaller>? logger = null)
    {
        if (handler is null)
        {
            _handler = new SocketsHttpHandler();
            _ownsHandler = true;
        }
        else
        {
            _handler = handler;
            _ownsHandler = false;
        }

        _httpClient = new HttpClient(_handler, disposeHandler: false)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        _logger = logger ?? NullLogger<RestCaller>.Instance;
    }

    /// <summary>
    /// Sends the REST call. There is no numeric timeout.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The final URL.</param>
    /// <param name="headers">The credential headers.</param>
    /// <param name="body">The JSON body, or <see langword="null"/> when the call has no body.</param>
    /// <param name="cancellationToken">The token to cancel the call.</param>
    /// <returns>The status and body, or a client error when the server cannot be reached.</returns>
    public async Task<TransportResult> SendAsync(
        string method,
        string url,
        IReadOnlyDictionary<string, string> headers,
        string? body,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(headers);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            return TransportResult.Unreachable();
        }

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
            foreach (var header in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    throw new InvalidOperationException($"The header '{header.Key}' could not be added.");
                }
            }

            if (body is not null)
            {
                var content = new StringContent(body, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = TransportResult.FromHttp((int)response.StatusCode, responseBody);
            _logger.LogInformation(
                "REST {Method} completed. Status {Status}. Succeeded {Succeeded}.",
                method,
                result.StatusCode,
                result.Succeeded);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            _logger.LogInformation("REST {Method} could not reach the server. ExceptionType {ExceptionType}.", method, ex.GetType().Name);
            return TransportResult.Unreachable();
        }
    }

    /// <summary>
    /// Disposes the HTTP client and the owned handler.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        if (_ownsHandler)
        {
            _handler.Dispose();
        }
    }

    #region private

    private readonly HttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHandler;
    private readonly ILogger<RestCaller> _logger;

    #endregion
}
