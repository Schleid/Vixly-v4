using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsPriceList;

// =========================================================
// FILE: SaveOperationsPriceListResponse.cs
//
// PURPOSE:
// Adds a price list, or schedules a new version of one, and takes it in or out of use (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsPriceListResponse
{
    private SaveOperationsPriceListResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CardId { get; private init; }

    public static SaveOperationsPriceListResponse Success(Guid cardId)
    {
        return new SaveOperationsPriceListResponse { Succeeded = true, CardId = cardId };
    }

    public static SaveOperationsPriceListResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsPriceListResponse { Error = error };
    }
}
