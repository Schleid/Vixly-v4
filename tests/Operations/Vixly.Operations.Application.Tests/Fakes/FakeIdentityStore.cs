using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeIdentityStore.cs
//
// PURPOSE:
// In-memory staff identity data of one tenant.
//
// RESPONSIBILITIES:
// - Keep users, sessions, pending sign-ins, links, audit and brand
// - Count saves
//
// IMPORTANT NOTES:
// - Tests only. Nothing is written until SaveChangesAsync, as with EF
// =========================================================
internal sealed class FakeIdentityStore : IOperationsUserRepository, IOperationsSessionRepository, IOperationsSignInChallengeRepository, IOperationsAccountTokenRepository, IOperationsAuditRepository, IOperationsBrandRepository, IOperationsSettingsRepository, IOperationsUnitOfWork
{
    private readonly List<object> _pending = new();

    public List<OperationsUser> Users { get; } = new();

    public List<OperationsUserSession> Sessions { get; } = new();

    public List<OperationsSignInChallenge> Challenges { get; } = new();

    public List<OperationsAccountToken> Tokens { get; } = new();

    public List<OperationsAuditEntry> Audit { get; } = new();

    public List<OperationsTenantBrand> Brands { get; } = new();

    public int Saves { get; private set; }

    public void Add(OperationsUser user) => _pending.Add(user);

    public void Add(OperationsUserSession session) => _pending.Add(session);

    public void Add(OperationsSignInChallenge challenge) => _pending.Add(challenge);

    public void Add(OperationsAccountToken token) => _pending.Add(token);

    public void Add(OperationsAuditEntry entry) => _pending.Add(entry);

    public void Add(OperationsTenantBrand brand) => _pending.Add(brand);

    public Vixly.Operations.Domain.Settings.OperationsTenantSettings? Settings { get; set; }

    public void Add(Vixly.Operations.Domain.Settings.OperationsTenantSettings settings) => Settings = settings;

    Task<Vixly.Operations.Domain.Settings.OperationsTenantSettings?> IOperationsSettingsRepository.FindAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);

    public Task<OperationsUser?> GetAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));

    public Task<OperationsUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) => Task.FromResult(Users.FirstOrDefault(user => user.Email == email));

    public Task<bool> AnyAsync(CancellationToken cancellationToken) => Task.FromResult(Users.Count > 0);

    public Task<(IReadOnlyList<OperationsUser> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        List<OperationsUser> matches = Users
            .Where(user => string.IsNullOrWhiteSpace(search) || user.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || user.Email.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(user => user.FullName, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult<(IReadOnlyList<OperationsUser>, int)>((matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(), matches.Count));
    }

    Task<OperationsUserSession?> IOperationsSessionRepository.FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Sessions.FirstOrDefault(session => session.TokenHash == tokenHash));

    Task<OperationsUserSession?> IOperationsSessionRepository.GetAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(Sessions.FirstOrDefault(session => session.Id == sessionId));

    public Task<IReadOnlyList<OperationsUserSession>> ListOpenAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsUserSession>>(Sessions.Where(session => session.UserId == userId && !session.IsEnded).ToList());

    Task<OperationsSignInChallenge?> IOperationsSignInChallengeRepository.FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Challenges.FirstOrDefault(challenge => challenge.TokenHash == tokenHash));

    Task<OperationsAccountToken?> IOperationsAccountTokenRepository.FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task<OperationsTenantBrand?> FindAsync(CancellationToken cancellationToken) => Task.FromResult(Brands.FirstOrDefault());

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (object item in _pending)
        {
            switch (item)
            {
                case OperationsUser user: Users.Add(user); break;
                case OperationsUserSession session: Sessions.Add(session); break;
                case OperationsSignInChallenge challenge: Challenges.Add(challenge); break;
                case OperationsAccountToken token: Tokens.Add(token); break;
                case OperationsAuditEntry entry: Audit.Add(entry); break;
                case OperationsTenantBrand brand: Brands.Add(brand); break;
            }
        }

        _pending.Clear();
        Saves++;
        return Task.FromResult(true);
    }

    public void DiscardChanges() => _pending.Clear();
}
