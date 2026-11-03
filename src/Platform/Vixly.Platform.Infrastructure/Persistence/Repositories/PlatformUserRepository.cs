using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformUserRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformUserRepository.
//
// RESPONSIBILITIES:
// - Load whole users (owned account and second factors come
//   automatically; grants and memberships are included)
// - Answer email and SuperUser count queries
// - Find the owner of a passkey (unique index on CredentialId)
//
// IMPORTANT NOTES:
// - Tracked loads: these are for commands. Screens use
//   no-tracking projections (P-1)
// - Split query avoids a cartesian product across the child
//   collections
// =========================================================
internal sealed class PlatformUserRepository : IPlatformUserRepository
{
    private readonly PlatformDbContext _context;

    public PlatformUserRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformUser?> GetAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return WholeUsers().FirstOrDefaultAsync(user => user.Id == platformUserId, cancellationToken);
    }

    public Task<PlatformUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        return WholeUsers().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        return _context.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<PlatformUser?> FindByPasskeyAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentialId);

        return WholeUsers().FirstOrDefaultAsync(
            user => user.Passkeys.Any(passkey => passkey.CredentialId == credentialId),
            cancellationToken);
    }

    public Task<int> CountOtherActiveSuperUsersAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        return _context.Users.CountAsync(
            user => user.Id != excludedUserId
                && user.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser
                && user.Account.Status == PlatformUserAccountStatus.Active,
            cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
    {
        return _context.Users.AnyAsync(cancellationToken);
    }

    // A plain command: SQL Server refuses NEXT VALUE FOR inside the subquery EF would wrap around SqlQueryRaw.
    public async Task<string> NextEmployeeNumberAsync(CancellationToken cancellationToken)
    {
        System.Data.Common.DbConnection connection = _context.Database.GetDbConnection();
        bool opened = connection.State != System.Data.ConnectionState.Open;

        if (opened)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using System.Data.Common.DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [" + PlatformDatabase.Schema + "].[" + PlatformDatabase.EmployeeNumberSequence + "]";
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

            return PlatformEmployeeNumber.Format(Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture));
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    public void Add(PlatformUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _context.Users.Add(user);
    }

    private IQueryable<PlatformUser> WholeUsers()
    {
        return _context.Users
            .Include(user => user.RoleAssignments)
            .Include(user => user.Departments)
            .AsSplitQuery();
    }
}
