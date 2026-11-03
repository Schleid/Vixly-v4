using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Plans.GetOperationsShipmentCap;

// =========================================================
// FILE: GetOperationsShipmentCapResponse.cs
//
// PURPOSE:
// Gives the company's shipments this month against its plan's cap, for the workspace banner (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsShipmentCapResponse
{
    private GetOperationsShipmentCapResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Vixly.Operations.Application.Plans.OperationsShipmentCapStatus? Status { get; private init; }

    public static GetOperationsShipmentCapResponse Success(Vixly.Operations.Application.Plans.OperationsShipmentCapStatus? status)
    {
        return new GetOperationsShipmentCapResponse { Succeeded = true, Status = status };
    }

    public static GetOperationsShipmentCapResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsShipmentCapResponse { Error = error };
    }
}
