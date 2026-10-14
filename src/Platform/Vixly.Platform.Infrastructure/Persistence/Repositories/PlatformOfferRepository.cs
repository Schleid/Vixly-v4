using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformOfferRepository.cs
//
// PURPOSE:
// Loads and stores offer links (D-S21).
//
// RESPONSIBILITIES:
// - Implement IPlatformOfferRepository with EF Core
//
// IMPORTANT NOTES:
// - Changes are saved by the unit of work
// =========================================================
internal sealed class PlatformOfferRepository : IPlatformOfferRepository
{
    private readonly PlatformDbContext _context;

    public PlatformOfferRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformOffer?> GetAsync(Guid offerId, CancellationToken cancellationToken)
    {
        return _context.Offers.FirstOrDefaultAsync(offer => offer.Id == offerId, cancellationToken);
    }

    public Task<PlatformOffer?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.Offers.FirstOrDefaultAsync(offer => offer.TokenHash == tokenHash, cancellationToken);
    }

    public void Add(PlatformOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        _context.Offers.Add(offer);
    }
}
