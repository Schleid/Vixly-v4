using Vixly.Operations.Application.Charges;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetMyOperationsPricing;

// =========================================================
// FILE: GetMyOperationsPricingResponse.cs
//
// PURPOSE:
// The prices a signed-in customer sees on the portal's Pricing page (owner, 2026-10-21): the regular prices and their own.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsPricingResponse
{
    private GetMyOperationsPricingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsPricingView? Pricing { get; private init; }

    public static GetMyOperationsPricingResponse Success(OperationsPricingView? pricing)
    {
        return new GetMyOperationsPricingResponse { Succeeded = true, Pricing = pricing };
    }

    public static GetMyOperationsPricingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsPricingResponse { Error = error };
    }
}
