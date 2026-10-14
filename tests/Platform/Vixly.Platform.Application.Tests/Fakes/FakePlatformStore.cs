using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Audit;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakePlatformStore.cs
//
// PURPOSE:
// An in-memory stand-in for every Platform persistence port,
// for Application tests.
//
// RESPONSIBILITIES:
// - Hold users, sessions, audit entries, challenges, account
//   links and reset requests in lists; record sent mail
// - Count saves and simulate a concurrency conflict on demand
// - Answer permission snapshots from the stored users and
//   roles, and the user and role projections
//
// IMPORTANT NOTES:
// - Hand-written; no mocking library (Slice 0 convention)
// =========================================================
internal sealed class FakePlatformStore :
    IPlatformUserRepository,
    IPlatformSessionRepository,
    IPlatformAuditRepository,
    IPlatformUnitOfWork,
    IPlatformPermissionReader,
    IPlatformRequestMetadata,
    IPlatformSignInChallengeRepository,
    IPlatformAccountTokenRepository,
    IPlatformPasswordResetRequestRepository,
    IPlatformPasskeyCeremonyRepository,
    IPlatformIdentityMailer,
    IPlatformUserQueries,
    IPlatformRoleRepository,
    IPlatformRoleQueries,
    IPlatformDepartmentRepository,
    IPlatformDepartmentQueries,
    IPlatformAuditQueries
{
    public List<PlatformUser> Users { get; } = new();

    public List<PlatformRole> Roles { get; } = new();

    public List<PlatformDepartment> Departments { get; } = new();

    public List<PlatformUserSession> Sessions { get; } = new();

    public List<PlatformAuditEntry> AuditEntries { get; } = new();

    public List<PlatformSignInChallenge> Challenges { get; } = new();

    public List<PlatformPasskeyCeremony> Ceremonies { get; } = new();

    public List<PlatformAccountToken> AccountTokens { get; } = new();

    public List<PlatformPasswordResetRequest> ResetRequests { get; } = new();

    public List<(string Kind, Guid UserId, string Value)> SentMail { get; } = new();

    public int Saves { get; private set; }

    public bool FailNextSave { get; set; }

    public string? ClientIp => "203.0.113.9";

    public string? UserAgent => "tests";

    public string? CorrelationId => "corr-1";

    public Task<PlatformUser?> GetAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == platformUserId));
    }

    public Task<PlatformUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Email == email));
    }

    public Task<PlatformUser?> FindByPasskeyAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Passkeys.Any(passkey => passkey.Matches(credentialId))));
    }

    Task<PlatformPasskeyCeremony?> IPlatformPasskeyCeremonyRepository.FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Ceremonies.FirstOrDefault(ceremony => ceremony.TokenHash == tokenHash));
    }

    public void Add(PlatformPasskeyCeremony ceremony)
    {
        Ceremonies.Add(ceremony);
    }

    public Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.Any(user => user.Email == email));
    }

    public Task<int> CountOtherActiveSuperUsersAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.Count(user => user.Id != excludedUserId && user.IsActiveSuperUser));
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.Count > 0);
    }

    public void Add(PlatformUser user)
    {
        Users.Add(user);
    }

    Task<PlatformUserSession?> IPlatformSessionRepository.GetAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sessions.FirstOrDefault(session => session.Id == sessionId));
    }

    public Task<PlatformUserSession?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sessions.FirstOrDefault(session => session.TokenHash == tokenHash));
    }

    public Task<IReadOnlyList<PlatformUserSession>> ListOpenAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformUserSession> open = Sessions
            .Where(session => session.PlatformUserId == platformUserId && !session.IsEnded)
            .ToList();

        return Task.FromResult(open);
    }

    public void Add(PlatformUserSession session)
    {
        Sessions.Add(session);
    }

    public void Add(PlatformAuditEntry entry)
    {
        AuditEntries.Add(entry);
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            return Task.FromResult(false);
        }

        Saves++;
        return Task.FromResult(true);
    }

    public int Discards { get; private set; }

    public void DiscardChanges()
    {
        Discards++;
    }

    public Task<PlatformPermissionSnapshot?> ReadAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        PlatformUser? user = Users.FirstOrDefault(candidate => candidate.Id == platformUserId);

        if (user is null)
        {
            return Task.FromResult<PlatformPermissionSnapshot?>(null);
        }

        IReadOnlyCollection<Guid> roleIds = user.RolesInForce(utcNow);
        HashSet<string> codes = Roles
            .Where(role => role.IsActive && roleIds.Contains(role.Id))
            .SelectMany(role => role.Permissions.Select(permission => permission.PermissionCode))
            .ToHashSet(StringComparer.Ordinal);

        return Task.FromResult<PlatformPermissionSnapshot?>(
            new PlatformPermissionSnapshot(user.Account.IsActive, user.AuthorityLevel, codes));
    }

    Task<PlatformSignInChallenge?> IPlatformSignInChallengeRepository.FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Challenges.FirstOrDefault(challenge => challenge.TokenHash == tokenHash));
    }

    public void Add(PlatformSignInChallenge challenge)
    {
        Challenges.Add(challenge);
    }

    Task<PlatformAccountToken?> IPlatformAccountTokenRepository.FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(AccountTokens.FirstOrDefault(token => token.TokenHash == tokenHash));
    }

    public Task<IReadOnlyList<PlatformAccountToken>> ListUnusedAsync(
        Guid platformUserId,
        PlatformAccountTokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformAccountToken> unused = AccountTokens
            .Where(token => token.PlatformUserId == platformUserId
                && token.Purpose == purpose
                && token.UsedUtc is null
                && token.RevokedUtc is null)
            .ToList();

        return Task.FromResult(unused);
    }

    public void Add(PlatformAccountToken token)
    {
        AccountTokens.Add(token);
    }

    public Task<int> CountSinceAsync(EmailAddress email, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult(ResetRequests.Count(request => request.Email == email && request.CreatedUtc >= sinceUtc));
    }

    public Task<int> CountFromIpSinceAsync(string ipAddress, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult(ResetRequests.Count(request => request.IpAddress == ipAddress && request.CreatedUtc >= sinceUtc));
    }

    public void Add(PlatformPasswordResetRequest request)
    {
        ResetRequests.Add(request);
    }

    public Task SendInvitationAsync(PlatformUser user, string token, CancellationToken cancellationToken)
    {
        SentMail.Add(("invitation", user.Id, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(PlatformUser user, string token, CancellationToken cancellationToken)
    {
        SentMail.Add(("reset", user.Id, token));
        return Task.CompletedTask;
    }

    public Task SendSecurityNoticeAsync(PlatformUser user, string noticeCode, CancellationToken cancellationToken)
    {
        SentMail.Add(("notice", user.Id, noticeCode));
        return Task.CompletedTask;
    }

    public PlatformUserListFilter? LastListFilter { get; private set; }

    public Task<PagedResult<PlatformUserListItem>> ListAsync(
        PlatformUserListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        LastListFilter = filter;
        List<PlatformUserListItem> items = Users
            .Where(user => filter.Status is null || user.Account.Status == filter.Status)
            .Where(user => filter.Search is null || user.Email.Value.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
            .Select(user => new PlatformUserListItem(user.Id, user.FirstName, user.LastName, user.Email.Value, user.Account.Status, user.AuthorityLevel, user.Account.LastSignInUtc))
            .ToList();

        return Task.FromResult(new PagedResult<PlatformUserListItem>(items.Skip(page.Skip).Take(page.PageSize).ToList(), page, items.Count));
    }

    public Task<PlatformUserActionFacts?> GetActionFactsAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformUser? user = Users.FirstOrDefault(candidate => candidate.Id == platformUserId);

        return Task.FromResult(user is null
            ? null
            : new PlatformUserActionFacts(
                user.Id,
                user.Account.Status,
                user.AuthorityLevel,
                user.HasSecondFactor,
                Sessions.Count(session => session.PlatformUserId == user.Id && session.IsValidAt(utcNow))));
    }

    public Task<PlatformUserDetail?> GetAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformUser? user = Users.FirstOrDefault(candidate => candidate.Id == platformUserId);

        return Task.FromResult(user is null
            ? null
            : new PlatformUserDetail { PlatformUserId = user.Id, Email = user.Email.Value, Status = user.Account.Status });
    }
    Task<PlatformRole?> IPlatformRoleRepository.GetAsync(Guid platformRoleId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Roles.FirstOrDefault(role => role.Id == platformRoleId));
    }

    public Task<IReadOnlyList<PlatformRole>> ListSystemAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformRole>>(Roles.Where(role => role.IsSystem).ToList());
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Roles.Any(role =>
            string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase) && role.Id != excludedRoleId));
    }

    public void Add(PlatformRole role)
    {
        Roles.Add(role);
    }

    Task<IReadOnlyList<PlatformRoleListItem>> IPlatformRoleQueries.ListAsync(
        bool includeInactive,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformRoleListItem>>(Roles
            .Where(role => includeInactive || role.IsActive)
            .Select(role => new PlatformRoleListItem(
                role.Id,
                role.Name,
                role.SystemKey,
                role.IsActive,
                Codes(role),
                Users.Count(user => user.RolesInForce(utcNow).Contains(role.Id))))
            .ToList());
    }

    Task<PlatformRoleDetail?> IPlatformRoleQueries.GetAsync(
        Guid platformRoleId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        PlatformRole? role = Roles.FirstOrDefault(candidate => candidate.Id == platformRoleId);

        return Task.FromResult(role is null
            ? null
            : new PlatformRoleDetail
            {
                PlatformRoleId = role.Id,
                Name = role.Name,
                SystemKey = role.SystemKey,
                IsActive = role.IsActive,
                PermissionCodes = Codes(role)
            });
    }

    public Task<IReadOnlyList<PlatformPermissionHolderItem>> ListPermissionHoldersAsync(
        string permissionCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        IEnumerable<PlatformPermissionHolderItem> superUsers = Users
            .Where(user => user.IsActiveSuperUser)
            .Select(user => new PlatformPermissionHolderItem(user.Id, user.FirstName, user.LastName, user.Email.Value, user.AuthorityLevel, null, null, null));
        IEnumerable<PlatformPermissionHolderItem> throughRoles =
            from user in Users
            where user.Account.IsActive
            from grant in user.RoleAssignments
            where grant.IsInForce(utcNow)
            join role in Roles on grant.PlatformRoleId equals role.Id
            where role.Grants(permissionCode)
            select new PlatformPermissionHolderItem(user.Id, user.FirstName, user.LastName, user.Email.Value, user.AuthorityLevel, role.Name, role.SystemKey, grant.ExpiresUtc);

        return Task.FromResult<IReadOnlyList<PlatformPermissionHolderItem>>(superUsers.Concat(throughRoles).ToList());
    }

    public Task<IReadOnlyList<PlatformUserRoleItem>> ListUserGrantsAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        PlatformUser? user = Users.FirstOrDefault(candidate => candidate.Id == platformUserId);
        IEnumerable<PlatformUserRoleItem> grants = user is null
            ? Enumerable.Empty<PlatformUserRoleItem>()
            : from grant in user.RoleAssignments
              where grant.IsInForce(utcNow)
              join role in Roles on grant.PlatformRoleId equals role.Id
              select new PlatformUserRoleItem(role.Id, role.Name, role.SystemKey, role.IsActive, grant.CreatedUtc, grant.ExpiresUtc);

        return Task.FromResult<IReadOnlyList<PlatformUserRoleItem>>(grants.ToList());
    }

    private static List<string> Codes(PlatformRole role)
    {
        return role.Permissions.Select(permission => permission.PermissionCode).OrderBy(code => code, StringComparer.Ordinal).ToList();
    }
    Task<PlatformDepartment?> IPlatformDepartmentRepository.GetAsync(Guid platformDepartmentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Departments.FirstOrDefault(department => department.Id == platformDepartmentId));
    }

    Task<bool> IPlatformDepartmentRepository.NameExistsAsync(string name, Guid? excludedDepartmentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Departments.Any(department =>
            string.Equals(department.Name, name, StringComparison.OrdinalIgnoreCase) && department.Id != excludedDepartmentId));
    }

    public void Add(PlatformDepartment department)
    {
        Departments.Add(department);
    }

    Task<IReadOnlyList<PlatformDepartmentListItem>> IPlatformDepartmentQueries.ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformDepartmentListItem>>(Departments
            .Where(department => includeInactive || department.IsActive)
            .OrderBy(department => department.Name, StringComparer.Ordinal)
            .Select(department => new PlatformDepartmentListItem(
                department.Id,
                department.Name,
                department.Description,
                department.IsActive,
                Users.Count(user => user.Departments.Any(membership => membership.PlatformDepartmentId == department.Id))))
            .ToList());
    }

    Task<PlatformDepartmentDetail?> IPlatformDepartmentQueries.GetAsync(Guid platformDepartmentId, CancellationToken cancellationToken)
    {
        PlatformDepartment? department = Departments.FirstOrDefault(candidate => candidate.Id == platformDepartmentId);

        return Task.FromResult(department is null
            ? null
            : new PlatformDepartmentDetail { PlatformDepartmentId = department.Id, Name = department.Name, IsActive = department.IsActive });
    }

    public Task<IReadOnlyList<PlatformUserDepartmentItem>> ListUserMembershipsAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        PlatformUser? user = Users.FirstOrDefault(candidate => candidate.Id == platformUserId);
        IEnumerable<PlatformUserDepartmentItem> memberships = user is null
            ? Enumerable.Empty<PlatformUserDepartmentItem>()
            : from membership in user.Departments
              join department in Departments on membership.PlatformDepartmentId equals department.Id
              select new PlatformUserDepartmentItem(department.Id, department.Name, department.IsActive, membership.IsPrimary);

        return Task.FromResult<IReadOnlyList<PlatformUserDepartmentItem>>(memberships.ToList());
    }
    public PlatformAuditFilter? LastAuditFilter { get; private set; }

    Task<PagedResult<PlatformAuditListItem>> IPlatformAuditQueries.ListAsync(
        PlatformAuditFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        LastAuditFilter = filter;
        List<PlatformAuditListItem> items = AuditEntries
            .Where(entry => filter.ActionCode is null || entry.ActionCode == filter.ActionCode)
            .Select(entry => new PlatformAuditListItem(entry.Id, entry.CreatedUtc, entry.ActorUserId, null, entry.ActionCode, entry.TargetType, entry.TargetId, null, null, entry.IpAddress))
            .ToList();

        return Task.FromResult(new PagedResult<PlatformAuditListItem>(items, page, items.Count));
    }

    Task<PlatformAuditEntryDetail?> IPlatformAuditQueries.GetAsync(Guid auditEntryId, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlatformAuditEntryDetail?>(null);
    }
}
