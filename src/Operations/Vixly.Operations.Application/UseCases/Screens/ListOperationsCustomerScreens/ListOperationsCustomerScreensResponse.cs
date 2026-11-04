using Vixly.Operations.Application.Screens;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.ListOperationsCustomerScreens;

// =========================================================
// FILE: ListOperationsCustomerScreensResponse.cs
//
// PURPOSE:
// The customer screens of the warehouses where the person serves or takes payments (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsCustomerScreensResponse
{
    private ListOperationsCustomerScreensResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsScreenRow> Screens { get; private init; } = Array.Empty<OperationsScreenRow>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static ListOperationsCustomerScreensResponse Success(IReadOnlyList<OperationsScreenRow> screens, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new ListOperationsCustomerScreensResponse { Succeeded = true, Screens = screens, Warehouses = warehouses };
    }

    public static ListOperationsCustomerScreensResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsCustomerScreensResponse { Error = error };
    }
}
