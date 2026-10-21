using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.OpenOperationsSupplyCount;

// =========================================================
// FILE: OpenOperationsSupplyCountResponse.cs
//
// PURPOSE:
// A stock count opened at a warehouse (Slice 5E, S-7): every active supply, or one category.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsSupplyCountResponse
{
    private OpenOperationsSupplyCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CountId { get; private init; }

    public static OpenOperationsSupplyCountResponse Success(Guid countId)
    {
        return new OpenOperationsSupplyCountResponse { Succeeded = true, CountId = countId };
    }

    public static OpenOperationsSupplyCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsSupplyCountResponse { Error = error };
    }
}
