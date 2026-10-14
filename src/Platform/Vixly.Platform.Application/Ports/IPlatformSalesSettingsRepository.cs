using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSalesSettingsRepository.cs
//
// PURPOSE:
// Loads and stores the Sales settings (D-S22).
//
// RESPONSIBILITIES:
// - Read the one settings row, if management has saved it
// - Add it the first time
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// - No row means nothing is set: every discount needs approval
// =========================================================
public interface IPlatformSalesSettingsRepository
{
    Task<PlatformSalesSettings?> GetAsync(CancellationToken cancellationToken);

    void Add(PlatformSalesSettings settings);
}
