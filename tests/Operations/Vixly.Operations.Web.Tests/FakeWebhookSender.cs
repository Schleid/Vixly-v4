using System.Collections.Concurrent;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeWebhookSender.cs
//
// PURPOSE:
// Stands in for the companies' webhook receivers (Slice 10): keeps what was posted, answers what a test sets.
//
// RESPONSIBILITIES:
// - Keep each post (address, headers, body); answer 200, or the next statuses a test queues
//
// IMPORTANT NOTES:
// - Tests only; never reaches the network
// =========================================================
internal sealed class FakeWebhookSender : IOperationsWebhookSender
{
    public ConcurrentQueue<(string Url, IReadOnlyDictionary<string, string> Headers, string Body)> Posts { get; } = new();

    public ConcurrentQueue<int> NextStatuses { get; } = new();

    public Task<OperationsWebhookResult> PostAsync(string url, IReadOnlyDictionary<string, string> headers, string body, CancellationToken cancellationToken)
    {
        Posts.Enqueue((url, new Dictionary<string, string>(headers, StringComparer.Ordinal), body));
        int status = NextStatuses.TryDequeue(out int next) ? next : 200;
        return Task.FromResult(new OperationsWebhookResult(status, status is >= 200 and < 300 ? null : "status"));
    }
}
