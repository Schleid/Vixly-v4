using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyClientDisconnects.cs
//
// PURPOSE:
// Treats a request the browser abandoned (closed tab, second click, lost connection) as normal, not as an error (ADR-0019).
//
// RESPONSIBILITIES:
// - Catch the cancellation raised because the client went away, including the database error SqlClient raises instead
//   ("Operation cancelled by user") when a query is cancelled mid-read
// - Log it at Debug and answer 499 when nothing was sent yet
//
// IMPORTANT NOTES:
// - Only when the request itself was aborted; any other cancellation still propagates and is logged as an error
// - Commits are never cancelled (PlatformUnitOfWork), so nothing is left half done
// =========================================================
public static class VixlyClientDisconnects
{
    public const int ClientClosedRequest = 499;

    private static readonly Action<ILogger, string, Exception?> LogAbandoned =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "RequestAbandoned"), "The client abandoned the request {Path}");

    public static WebApplication UseVixlyClientDisconnects(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Vixly.Hosting.ClientDisconnects");

        app.Use((context, next) => HandleAsync(context, next, logger));

        return app;
    }

    public static async Task HandleAsync(HttpContext context, RequestDelegate next, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            await next(context);
        }
        catch (Exception exception) when (context.RequestAborted.IsCancellationRequested && IsCancellation(exception))
        {
            LogAbandoned(logger, context.Request.Path.Value ?? string.Empty, null);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
    }

    // A cancelled query surfaces as OperationCanceledException, or as a DbException (SqlException "Operation cancelled by user") from a reader cancelled mid-read.
    private static bool IsCancellation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException or DbException)
            {
                return true;
            }
        }

        return false;
    }
}
