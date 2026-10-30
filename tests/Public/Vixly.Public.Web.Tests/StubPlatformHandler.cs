using System.Net;
using System.Text;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: StubPlatformHandler.cs
//
// PURPOSE:
// Stands in for Platform.Api behind the public site’s HttpClient.
//
// RESPONSIBILITIES:
// - Answer from StubPlatformState
// - Record each request
//
// IMPORTANT NOTES:
// - A new handler per client rotation; the state is shared
// =========================================================
internal sealed class StubPlatformHandler : HttpMessageHandler
{
    private readonly StubPlatformState _state;

    public StubPlatformHandler(StubPlatformState state)
    {
        _state = state;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.PathAndQuery.TrimStart('/');
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        string? caller = request.Headers.TryGetValues("X-Vixly-Caller", out IEnumerable<string>? values) ? values.Single() : null;

        _state.Requests.Enqueue(new StubPlatformRequest(request.Method.Method, path, body, caller));

        (HttpStatusCode Status, string Json)? answer = _state.Find(request.Method, path);

        return answer is { } found
            ? new HttpResponseMessage(found.Status) { Content = new StringContent(found.Json, Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}
