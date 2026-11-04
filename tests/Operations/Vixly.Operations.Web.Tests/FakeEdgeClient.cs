using System.Collections.Concurrent;
using Vixly.Operations.Application.Branding;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeEdgeClient.cs
//
// PURPOSE:
// Records the brands sent to the Cloudflare Worker instead of sending them.
// RESPONSIBILITIES:
// - Keep each brand sent; refuse the slugs listed in Failing
//
// IMPORTANT NOTES:
// - Registered by OperationsWebTestHost in place of the real client
// =========================================================
internal sealed class FakeEdgeClient : IOperationsEdgeClient
{
    public ConcurrentQueue<OperationsEdgeBrand> Brands { get; } = new();

    public HashSet<string> Failing { get; } = new(StringComparer.Ordinal);

    public Task<OperationsEdgeSendResult> SendBrandAsync(OperationsEdgeBrand brand, CancellationToken cancellationToken)
    {
        if (Failing.Contains(brand.Slug))
        {
            throw new HttpRequestException("refused");
        }

        Brands.Enqueue(brand);
        return Task.FromResult(OperationsEdgeSendResult.Sent);
    }
}
