using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPromotionRepository.cs
//
// PURPOSE:
// Loads and stores promotions (D-S20).
//
// RESPONSIBILITIES:
// - Implement IPlatformPromotionRepository with EF Core
//
// IMPORTANT NOTES:
// - Changes are saved by the unit of work
// =========================================================
internal sealed class PlatformPromotionRepository : IPlatformPromotionRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPromotionRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformPromotion?> GetAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        return _context.Promotions.FirstOrDefaultAsync(promotion => promotion.Id == promotionId, cancellationToken);
    }

    public Task<PlatformPromotion?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _context.Promotions.FirstOrDefaultAsync(promotion => promotion.Code == code, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return _context.Promotions.AnyAsync(promotion => promotion.Code == code, cancellationToken);
    }

    public void Add(PlatformPromotion promotion)
    {
        ArgumentNullException.ThrowIfNull(promotion);

        _context.Promotions.Add(promotion);
    }
}
