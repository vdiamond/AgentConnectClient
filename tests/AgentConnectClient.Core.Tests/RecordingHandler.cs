using System.Net;
using System.Text;

namespace AgentConnectClient.Core.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    public int Calls { get; private set; }

    public List<RecordedRequest> Requests { get; } = [];

    public Func<RecordedRequest, HttpResponseMessage>? Responder { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string? authorization = null;
        if (request.Headers.TryGetValues("Authorization", out var authorizationValues))
        {
            authorization = string.Join(",", authorizationValues);
        }

        string? adminKey = null;
        if (request.Headers.TryGetValues("X-Admin-Key", out var adminValues))
        {
            adminKey = string.Join(",", adminValues);
        }

        var contentType = request.Content?.Headers.ContentType?.MediaType;
        var recorded = new RecordedRequest(
            request.Method.Method,
            request.RequestUri?.ToString() ?? "",
            authorization,
            adminKey,
            body,
            contentType);
        Requests.Add(recorded);
        if (Responder is not null)
        {
            return Responder(recorded);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json"),
        };
    }
}

internal sealed record RecordedRequest(
    string Method,
    string Url,
    string? Authorization,
    string? AdminKey,
    string? Body,
    string? ContentType);
