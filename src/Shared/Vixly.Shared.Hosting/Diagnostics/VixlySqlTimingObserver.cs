using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Vixly.Shared.Hosting.Diagnostics;

// =========================================================
// FILE: VixlySqlTimingObserver.cs
//
// PURPOSE:
// Adds each database command to the request that ran it (performance plan P-1).
//
// RESPONSIBILITIES:
// - Listen to Entity Framework's own diagnostic events, executed and failed commands, and add their time to the current request
//
// IMPORTANT NOTES:
// - No reference to Entity Framework: the event's Duration is read by name
// - Commands outside a request (background jobs) are not counted
// =========================================================
public sealed class VixlySqlTimingObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string ListenerName = "Microsoft.EntityFrameworkCore";

    private const string Executed = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";

    private const string Failed = "Microsoft.EntityFrameworkCore.Database.Command.CommandError";

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> DurationOf = new();

    private readonly IHttpContextAccessor _http;

    private readonly List<IDisposable> _subscriptions = new();

    private IDisposable? _all;

    public VixlySqlTimingObserver(IHttpContextAccessor http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public void Start() => _all ??= DiagnosticListener.AllListeners.Subscribe(this);

    public void OnNext(DiagnosticListener value)
    {
        if (value?.Name == ListenerName)
        {
            lock (_subscriptions)
            {
                _subscriptions.Add(value.Subscribe(this, name => name is Executed or Failed));
            }
        }
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value is not { } payload || _http.HttpContext?.Features.Get<VixlyRequestTimes>() is not { } times || !ReferenceEquals(times.Owner, this))
        {
            return;
        }

        if (DurationOf.GetOrAdd(payload.GetType(), type => type.GetProperty("Duration"))?.GetValue(payload) is TimeSpan duration)
        {
            times.AddSql(duration);
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void Dispose()
    {
        _all?.Dispose();

        lock (_subscriptions)
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
