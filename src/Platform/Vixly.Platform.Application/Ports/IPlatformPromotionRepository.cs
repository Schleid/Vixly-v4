using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPromotionRepository.cs
//
// PURPOSE:
// Loads and stores promotions (D-S20).
//
// RESPONSIBILITIES:
// - Find a promotion by id or code
// - Add a promotion
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; codes are unique, upper case
// =========================================================
public interface IPlatformPromotionRepository
{
    Task<PlatformPromotion?> GetAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<PlatformPromotion?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    void Add(PlatformPromotion promotion);
}
