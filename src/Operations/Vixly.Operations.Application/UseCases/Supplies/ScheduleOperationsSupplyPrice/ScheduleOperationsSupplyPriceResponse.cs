using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ScheduleOperationsSupplyPrice;

// =========================================================
// FILE: ScheduleOperationsSupplyPriceResponse.cs
//
// PURPOSE:
// A new price for a supply, from now or a later day (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ScheduleOperationsSupplyPriceResponse
{
    private ScheduleOperationsSupplyPriceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ScheduleOperationsSupplyPriceResponse Success()
    {
        return new ScheduleOperationsSupplyPriceResponse { Succeeded = true };
    }

    public static ScheduleOperationsSupplyPriceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ScheduleOperationsSupplyPriceResponse { Error = error };
    }
}
