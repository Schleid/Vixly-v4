using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.EstimateOperationsPublicShipment;

// =========================================================
// FILE: EstimateOperationsPublicShipmentResponse.cs
//
// PURPOSE:
// A visitor's estimate on the company's website (owner, 2026-10-30): a weight to a pickup destination with a published service.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EstimateOperationsPublicShipmentResponse
{
    private EstimateOperationsPublicShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Vixly.Operations.Application.Charges.OperationsEstimate? Estimate { get; private init; }

    public static EstimateOperationsPublicShipmentResponse Success(Vixly.Operations.Application.Charges.OperationsEstimate? estimate)
    {
        return new EstimateOperationsPublicShipmentResponse { Succeeded = true, Estimate = estimate };
    }

    public static EstimateOperationsPublicShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EstimateOperationsPublicShipmentResponse { Error = error };
    }
}
