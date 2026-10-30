using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformUserQueries.cs
//
// PURPOSE:
// EF implementation of IPlatformUserQueries.
//
// RESPONSIBILITIES:
// - Page the user list with a LIKE search on name and email
// - Project one user's detail in a few small queries
//
// IMPORTANT NOTES:
// - No tracking, projections only (P-1)
// - The email column has a value converter, so the search casts
//   it to its stored string for LIKE
// - Wildcards typed by the user are escaped
// - Open sessions are those not ended and not past their idle
//   or absolute limit at utcNow
// =========================================================
internal sealed class PlatformUserQueries : IPlatformUserQueries
{
    private readonly PlatformDbContext _context;

    public PlatformUserQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformUserListItem>> ListAsync(
        PlatformUserListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformUser> users = _context.Users.AsNoTracking();

        if (filter.Status is PlatformUserAccountStatus status)
        {
            users = users.Where(user => user.Account.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string pattern = "%" + EscapeLike(filter.Search.Trim()) + "%";
            users = users.Where(user =>
                EF.Functions.Like(user.FirstName, pattern, "\\")
                || EF.Functions.Like(user.LastName, pattern, "\\")
                || EF.Functions.Like((string)(object)user.Email, pattern, "\\"));
        }

        int total = await users.CountAsync(cancellationToken);

        List<PlatformUserListItem> items = await users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(user => new PlatformUserListItem(
                user.Id,
                user.FirstName,
                user.LastName,
                (string)(object)user.Email,
                user.Account.Status,
                user.AuthorityLevel,
                user.Account.LastSignInUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformUserListItem>(items, page, total);
    }

    public async Task<PlatformUserDetail?> GetAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        PlatformUser? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == platformUserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        DateTimeOffset idleCutoff = utcNow - PlatformIdentityLimits.SessionIdleTimeout - TimeSpan.FromMinutes(1);

        List<PlatformUserSessionItem> sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(session => session.PlatformUserId == platformUserId
                && session.EndedUtc == null
                && session.ExpiresUtc > utcNow
                && session.LastSeenUtc > idleCutoff)
            .OrderByDescending(session => session.LastSeenUtc)
            .Select(session => new PlatformUserSessionItem(
                session.Id,
                session.CreatedUtc,
                session.LastSeenUtc,
                session.IpAddress,
                session.UserAgent))
            .ToListAsync(cancellationToken);

        return new PlatformUserDetail
        {
            PlatformUserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email.Value,
            EmployeeNumber = user.EmployeeNumber,
            PhoneNumber = user.PhoneNumber?.Value,
            PreferredLanguage = user.PreferredLanguage.Value,
            AuthorityLevel = user.AuthorityLevel,
            Status = user.Account.Status,
            StatusReason = user.Account.StatusReason,
            StatusChangedUtc = user.Account.StatusChangedUtc,
            CreatedUtc = user.CreatedUtc,
            LastSignInUtc = user.Account.LastSignInUtc,
            HasAuthenticatorApp = user.AuthenticatorApp?.IsConfirmed == true,
            PasskeyCount = user.Passkeys.Count,
            Passkeys = user.Passkeys
                .OrderBy(passkey => passkey.CreatedUtc)
                .Select(passkey => new PlatformUserPasskeyItem(passkey.Id, passkey.Name, passkey.CreatedUtc, passkey.LastUsedUtc))
                .ToList(),
            UnusedRecoveryCodeCount = user.UnusedRecoveryCodeCount,
            OpenSessions = sessions
        };
    }

    public async Task<PlatformUserActionFacts?> GetActionFactsAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        DateTimeOffset idleCutoff = utcNow - PlatformIdentityLimits.SessionIdleTimeout - TimeSpan.FromMinutes(1);

        return await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == platformUserId)
            .Select(user => new PlatformUserActionFacts(
                user.Id,
                user.Account.Status,
                user.AuthorityLevel,
                user.Passkeys.Any() || (user.AuthenticatorApp != null && user.AuthenticatorApp.ConfirmedUtc != null),
                _context.UserSessions.Count(session => session.PlatformUserId == user.Id
                    && session.EndedUtc == null
                    && session.ExpiresUtc > utcNow
                    && session.LastSeenUtc > idleCutoff)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<PlatformSignInItem>> ListSignInsAsync(
        Guid? platformUserId,
        DateTimeOffset utcNow,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformUserSession> sessions = _context.UserSessions.AsNoTracking();

        if (platformUserId is { } userId)
        {
            sessions = sessions.Where(session => session.PlatformUserId == userId);
        }

        int total = await sessions.CountAsync(cancellationToken);
        var rows = await sessions
            .OrderByDescending(session => session.CreatedUtc)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Join(_context.Users, session => session.PlatformUserId, user => user.Id, (session, user) => new { session, user.FirstName, user.LastName })
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformSignInItem>(rows.Select(row => Item(row.session, row.FirstName + " " + row.LastName, utcNow)).ToList(), page, total);
    }

    // A session nobody ended stops at the first of its idle and absolute limits: shown as ended there.
    private static PlatformSignInItem Item(PlatformUserSession session, string name, DateTimeOffset utcNow)
    {
        DateTimeOffset idleAt = session.LastSeenUtc + PlatformIdentityLimits.SessionIdleTimeout;
        (DateTimeOffset limit, string reason) = idleAt < session.ExpiresUtc ? (idleAt, "idle_timeout") : (session.ExpiresUtc, "absolute_lifetime");
        (DateTimeOffset? outUtc, string? ended) = session.EndedUtc is { } endedUtc
            ? (endedUtc, Snake((session.EndReason ?? PlatformSessionEndReason.SignedOut).ToString()))
            : limit <= utcNow
                ? (limit, reason)
                : ((DateTimeOffset?)null, (string?)null);

        return new PlatformSignInItem(session.Id, session.PlatformUserId, name.Trim(), session.CreatedUtc, outUtc, ended, session.IpAddress, session.UserAgent, session.Country);
    }

    private static string Snake(string value) =>
        string.Concat(value.Select((letter, index) => index > 0 && char.IsUpper(letter) ? "_" + char.ToLowerInvariant(letter) : char.ToLowerInvariant(letter).ToString()));

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }
}
