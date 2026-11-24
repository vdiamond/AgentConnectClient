using System.Net.Http.Headers;
using System.Text;

namespace AgentConnectClient.Transport;

/// <summary>
/// Records the last non-success HTTP response so an MCP connection rejection can show the status and body.
/// </summary>
internal sealed class ResponseCaptureHandler : DelegatingHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseCaptureHandler"/> class.
    /// </summary>
    /// <param name="inner">The handler that performs the request. It is not disposed with this instance.</param>
    public ResponseCaptureHandler(HttpMessageHandler inner)
        : base(inner)
    {
    }

    /// <summary>
    /// Gets the last non-success response seen by this handler.
    /// </summary>
    public CapturedHttpError? LastNonSuccess { get; private set; }

    #region protected

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            LastNonSuccess = null;
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        LastNonSuccess = new CapturedHttpError((int)response.StatusCode, body);
        var mediaType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        var replacement = new StringContent(body, Encoding.UTF8);
        replacement.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
        response.Content = replacement;
        return response;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The shared inner handler outlives this per-call capture, so it is not disposed here.
    }

    #endregion
}

/// <summary>
/// An HTTP error status and the raw body that produced it.
/// </summary>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="Body">The raw body.</param>
internal sealed record CapturedHttpError(int StatusCode, string Body);
