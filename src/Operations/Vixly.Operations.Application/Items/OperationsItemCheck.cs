using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemCheck.cs
//
// PURPOSE:
// Stops a parcel before it is sealed in a bundle or a box, or leaves on a dispatch, while an item lacks its customs code or country of origin (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - When the company requires it: every item complete, and at least one item declared, for parcels created since it was switched on
//
// IMPORTANT NOTES:
// - Parcels created before the company switched it on are never stopped
// - Off by default: nothing changes until the company switches it on
// =========================================================
public sealed class OperationsItemCheck
{
    private readonly IOperationsItemRepository _items;

    private readonly IOperationsSettingsRepository _settings;

    public OperationsItemCheck(IOperationsItemRepository items, IOperationsSettingsRepository settings)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    // Null when every parcel may go; otherwise the first parcel and item to complete.
    public async Task<Error?> RefuseAsync(IReadOnlyCollection<OperationsParcel> parcels, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcels);

        if (await _settings.FindAsync(cancellationToken) is not { ItemsRequiredSinceUtc: { } since })
        {
            return null;
        }

        List<OperationsParcel> checkedParcels = parcels.Where(parcel => parcel.CreatedUtc >= since).ToList();

        if (checkedParcels.Count == 0)
        {
            return null;
        }

        IReadOnlyList<OperationsParcelItem> items = await _items.ListForParcelsAsync(checkedParcels, cancellationToken);

        foreach (OperationsParcel parcel in checkedParcels.OrderBy(parcel => parcel.Number, StringComparer.Ordinal))
        {
            List<OperationsParcelItem> own = items.Where(item => item.ParcelId == parcel.Id).ToList();

            if (own.Count == 0 && parcel.PreAlertId is { } preAlert)
            {
                own = items.Where(item => item.ParcelId == null && item.PreAlertId == preAlert).ToList();
            }

            if (own.Count == 0)
            {
                return OperationsItemErrors.Incomplete(parcel.Number ?? string.Empty, parcel.DeclaredContents ?? "-");
            }

            if (own.OrderBy(item => item.Line).FirstOrDefault(item => !item.IsComplete) is { } missing)
            {
                return OperationsItemErrors.Incomplete(parcel.Number ?? string.Empty, missing.Words);
            }
        }

        return null;
    }
}
