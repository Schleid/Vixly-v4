using System.Collections.Concurrent;
using System.Net;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: StubPlatformState.cs
//
// PURPOSE:
// What the stand-in Platform.Api answers and what it was asked.
//
// RESPONSIBILITIES:
// - Hold a canned answer per method and path
// - Record every request with its body and caller header
//
// IMPORTANT NOTES:
// - A path without an answer gets 503, as when Platform.Api is down
// =========================================================
internal sealed class StubPlatformState
{
    private readonly ConcurrentDictionary<string, (HttpStatusCode Status, string Json)> _answers = new(StringComparer.Ordinal);

    public ConcurrentQueue<StubPlatformRequest> Requests { get; } = new();

    public void Answer(HttpMethod method, string path, HttpStatusCode status, string json)
    {
        _answers[method.Method + " " + path] = (status, json);
    }

    public (HttpStatusCode Status, string Json)? Find(HttpMethod method, string path)
    {
        return _answers.TryGetValue(method.Method + " " + path, out (HttpStatusCode Status, string Json) answer) ? answer : null;
    }
}
