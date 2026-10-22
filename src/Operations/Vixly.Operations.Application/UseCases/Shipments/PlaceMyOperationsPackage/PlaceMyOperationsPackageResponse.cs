using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage;

// =========================================================
// FILE: PlaceMyOperationsPackageResponse.cs
//
// PURPOSE:
// A customer puts one of its packages into one of its shipments, moves it, or takes it out (Slice 4D; owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PlaceMyOperationsPackageResponse
{
    private PlaceMyOperationsPackageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static PlaceMyOperationsPackageResponse Success()
    {
        return new PlaceMyOperationsPackageResponse { Succeeded = true };
    }

    public static PlaceMyOperationsPackageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlaceMyOperationsPackageResponse { Error = error };
    }
}
