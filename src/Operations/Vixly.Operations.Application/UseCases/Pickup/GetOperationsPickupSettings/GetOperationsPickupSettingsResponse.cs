using Vixly.Operations.Domain.Pickup;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickupSettings;

// =========================================================
// FILE: GetOperationsPickupSettingsResponse.cs
//
// PURPOSE:
// Settings → Pickup: the proof asked, destination charges, storage at the destination (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPickupSettingsResponse
{
    private GetOperationsPickupSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsPickupProofs Proofs { get; private init; }

    public bool DestinationCharges { get; private init; }

    public bool DestinationStorage { get; private init; }

    public int FreeDays { get; private init; }

    public decimal? FeePerDay { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public bool CanEdit { get; private init; }

    public static GetOperationsPickupSettingsResponse Success(OperationsPickupProofs proofs, bool destinationCharges, bool destinationStorage, int freeDays, decimal? feePerDay, string currencyCode, bool canEdit)
    {
        return new GetOperationsPickupSettingsResponse { Succeeded = true, Proofs = proofs, DestinationCharges = destinationCharges, DestinationStorage = destinationStorage, FreeDays = freeDays, FeePerDay = feePerDay, CurrencyCode = currencyCode, CanEdit = canEdit };
    }

    public static GetOperationsPickupSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPickupSettingsResponse { Error = error };
    }
}
