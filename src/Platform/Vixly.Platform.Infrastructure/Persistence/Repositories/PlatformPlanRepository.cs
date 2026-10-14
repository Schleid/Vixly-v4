using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPlanRepository.cs
//
// PURPOSE:
// Loads and stores plans.
//
// RESPONSIBILITIES:
// - Load plans with translations, versions, limits and features
//
// IMPORTANT NOTES:
// - Versions are always loaded: a plan is small and every rule needs them
// =========================================================
internal sealed class PlatformPlanRepository : IPlatformPlanRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPlanRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformPlan?> GetAsync(Guid platformPlanId, CancellationToken cancellationToken)
    {
        return Plans().FirstOrDefaultAsync(plan => plan.Id == platformPlanId, cancellationToken);
    }

    public Task<PlatformPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Plans().FirstOrDefaultAsync(plan => plan.Code == code, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _context.Plans.AnyAsync(plan => plan.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformPlan>> ListChoosableAsync(bool publicOnly, CancellationToken cancellationToken)
    {
        List<PlatformPlan> plans = await Plans()
            .Where(plan => plan.Status == PlatformPlanStatus.Active && (!publicOnly || plan.IsPublic))
            .Where(plan => plan.Versions.Any(version => version.Status == PlatformPlanVersionStatus.Published))
            .OrderBy(plan => plan.DisplayOrder)
            .ThenBy(plan => plan.Code)
            .ToListAsync(cancellationToken);

        return plans;
    }

    public void Add(PlatformPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        _context.Plans.Add(plan);
    }

    private IQueryable<PlatformPlan> Plans()
    {
        return _context.Plans.Include(plan => plan.Versions);
    }
}
