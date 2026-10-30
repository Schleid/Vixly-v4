using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsAccountTokenRepository.cs
//
// PURPOSE:
// Emailed account links of the current tenant.
//
// RESPONSIBILITIES:
// - Implement IOperationsAccountTokenRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered by the query filter (D-F1)
// =========================================================
internal sealed class OperationsAccountTokenRepository : IOperationsAccountTokenRepository
{
    private readonly OperationsDbContext _context;

    public OperationsAccountTokenRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsAccountToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        _context.AccountTokens.Add(token);
    }

    public Task<OperationsAccountToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.AccountTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }
}
