namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsStepUpContinuationMiddleware.cs
//
// PURPOSE:
// Lets every staff action that asks for a fresh code continue after it (owner, 2026-11-04), without a change to each endpoint.
//
// RESPONSIBILITIES:
// - Watch the POSTs under /staff and, as their response starts, keep the form of one sent to the code page (uploads are not kept,
//   and drop anything kept before)
//
// IMPORTANT NOTES:
// - The endpoint decides whether a code is needed (its redirect to /staff/confirm); this only keeps what was sent
// - The code page itself is never kept
// =========================================================
public sealed class OperationsStepUpContinuationMiddleware
{
    private readonly RequestDelegate _next;

    private readonly OperationsStepUpContinuation _continuation;

    public OperationsStepUpContinuationMiddleware(RequestDelegate next, OperationsStepUpContinuation continuation)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _continuation = continuation ?? throw new ArgumentNullException(nameof(continuation));
    }

    public Task InvokeAsync(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        if (HttpMethods.IsPost(http.Request.Method)
            && http.Request.Path.StartsWithSegments("/staff")
            && !http.Request.Path.StartsWithSegments("/staff/confirm"))
        {
            http.Response.OnStarting(() =>
            {
                _continuation.KeepIfAsked(http);
                return Task.CompletedTask;
            });
        }

        return _next(http);
    }
}
