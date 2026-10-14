using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformOfferRepository.cs
//
// PURPOSE:
// Loads and stores offer links (D-S21).
//
// RESPONSIBILITIES:
// - Find an offer by id or by the hash of its link token
// - Add an offer
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// - Only token hashes are stored and searched
// =========================================================
public interface IPlatformOfferRepository
{
    Task<PlatformOffer?> GetAsync(Guid offerId, CancellationToken cancellationToken);

    Task<PlatformOffer?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(PlatformOffer offer);
}
