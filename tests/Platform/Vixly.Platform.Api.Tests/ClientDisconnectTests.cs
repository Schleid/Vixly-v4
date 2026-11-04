﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Shared.Hosting;

namespace Vixly.Platform.Api.Tests;

// =========================================================
// FILE: ClientDisconnectTests.cs
//
// PURPOSE:
// Proves a request the browser abandoned is not reported as an
// error, while any other cancellation still is (ADR-0019).
//
// RESPONSIBILITIES:
// - Aborted request: no exception, status 499
// - Cancellation not caused by the client: still thrown
//
// IMPORTANT NOTES:
// - Calls the middleware directly with a DefaultHttpContext
// =========================================================
public sealed class ClientDisconnectTests
{
    [Fact]
    public async Task AnAbandonedRequest_EndsQuietly_With499()
    {
        using CancellationTokenSource aborted = new();
        DefaultHttpContext context = new() { RequestAborted = aborted.Token };

        await VixlyClientDisconnects.HandleAsync(context, http =>
        {
            aborted.Cancel();
            http.RequestAborted.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, NullLogger.Instance);

        Assert.Equal(VixlyClientDisconnects.ClientClosedRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task ADatabaseQueryCancelledBecauseTheClientLeft_EndsQuietly_ButOtherDatabaseErrorsStillFail()
    {
        using CancellationTokenSource aborted = new();
        DefaultHttpContext context = new() { RequestAborted = aborted.Token };

        await VixlyClientDisconnects.HandleAsync(context, _ =>
        {
            aborted.Cancel();
            throw new CancelledQuery();
        }, NullLogger.Instance);

        Assert.Equal(VixlyClientDisconnects.ClientClosedRequest, context.Response.StatusCode);
        await Assert.ThrowsAsync<CancelledQuery>(() => VixlyClientDisconnects.HandleAsync(new DefaultHttpContext(), _ => throw new CancelledQuery(), NullLogger.Instance));
    }

    [Fact]
    public async Task OtherCancellations_StillFail()
    {
        DefaultHttpContext context = new();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            VixlyClientDisconnects.HandleAsync(context, _ => throw new OperationCanceledException(), NullLogger.Instance));
    }

    // What SqlClient throws for a query cancelled mid-read ("Operation cancelled by user").
    private sealed class CancelledQuery : System.Data.Common.DbException
    {
        public CancelledQuery()
            : base("Operation cancelled by user.")
        {
        }
    }
}
