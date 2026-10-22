using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices;

// =========================================================
// FILE: SaveOperationsShippingServicesResponse.cs
//
// PURPOSE:
// Saves which shipping services the tenant offers, and how (Slice 1E, D-O15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsShippingServicesResponse
{
    private SaveOperationsShippingServicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsShippingServicesResponse Success()
    {
        return new SaveOperationsShippingServicesResponse { Succeeded = true };
    }

    public static SaveOperationsShippingServicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsShippingServicesResponse { Error = error };
    }
}
