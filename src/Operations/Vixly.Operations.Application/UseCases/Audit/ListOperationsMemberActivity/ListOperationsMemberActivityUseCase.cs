using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Clients;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Audit.ListOperationsMemberActivity;

// =========================================================
// FILE: ListOperationsMemberActivityUseCase.cs
//
// PURPOSE:
// Member activity (owner, 2026-10-21): the company's sign-ins and sign-outs, with the device, the browser and the country, newest first.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.audit.view; the same staff who read the audit log
// - A session ends when signed out, ended by someone, or at its idle or absolute limit
// =========================================================
public sealed class ListOperationsMemberActivityUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSessionRepository _sessions;

    private readonly IOperationsUserRepository _users;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsIdleTimesReader _idle;

    public ListOperationsMemberActivityUseCase(
        OperationsAccess access,
        IOperationsSessionRepository sessions,
        IOperationsUserRepository users,
        TimeProvider timeProvider,
        IOperationsIdleTimesReader idle)
    {
        _idle = idle ?? throw new ArgumentNullException(nameof(idle));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsMemberActivityResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsMemberActivityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.AuditView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsMemberActivityResponse.Failure(refused);
        }

        (IReadOnlyList<OperationsUserSession> items, int total) = await _sessions.ListActivityAsync(request.UserId, Math.Max(request.Page, 1), PageSize, cancellationToken);
        Dictionary<Guid, string> names = new();

        foreach (Guid userId in items.Select(item => item.UserId).Distinct())
        {
            if (await _users.GetAsync(userId, cancellationToken) is { } user)
            {
                names[userId] = user.FullName;
            }
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        TimeSpan idle = (await IdleTimesAsync(cancellationToken)).Staff;
        return ListOperationsMemberActivityResponse.Success(items.Select(item => Line(item, names.GetValueOrDefault(item.UserId), utcNow, idle)).ToList(), total);
    }

    public const int PageSize = 50;

    // A session nobody ended stops at the first of its idle and absolute limits: shown as ended there, never as still signed in.
    // The idle time is the company's (owner, 2026-10-27).
    private static OperationsActivityLine Line(OperationsUserSession session, string? name, DateTimeOffset utcNow, TimeSpan idle)
    {
        DateTimeOffset idleAt = session.LastSeenUtc + idle;
        (DateTimeOffset limit, string reason) = idleAt < session.ExpiresUtc ? (idleAt, "idle_timeout") : (session.ExpiresUtc, "absolute_lifetime");
        (DateTimeOffset? outUtc, string? ended) = session.EndedUtc is { } endedUtc
            ? (endedUtc, Snake(session.EndReason?.ToString() ?? nameof(OperationsSessionEndReason.SignedOut)))
            : limit <= utcNow
                ? (limit, reason)
                : ((DateTimeOffset?)null, (string?)null);

        return new OperationsActivityLine(session.Id, session.UserId, name, session.CreatedUtc, outUtc, ended, session.DeviceName, session.DeviceNumber,
            VixlyUserAgents.Browser(session.UserAgent), session.Country, session.CountryChanged);
    }

    private static string Snake(string value) =>
        string.Concat(value.Select((letter, index) => index > 0 && char.IsUpper(letter) ? "_" + char.ToLowerInvariant(letter) : char.ToLowerInvariant(letter).ToString()));

    private async Task<Vixly.Operations.Application.Identity.OperationsIdleTimes> IdleTimesAsync(CancellationToken cancellationToken) =>
        await _idle.GetAsync(cancellationToken);
}
