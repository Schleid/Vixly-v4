using Vixly.Operations.Application.Charges;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsPublicPricing;

// =========================================================
// FILE: GetOperationsPublicPricingResponse.cs
//
// PURPOSE:
// The company's published prices on its public website (owner, 2026-10-21): the whole list on the Pricing page, only a yes or a no for the menu.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPublicPricingResponse
{
    private GetOperationsPublicPricingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Shown { get; private init; }

    public OperationsPricingView? Pricing { get; private init; }

    public static GetOperationsPublicPricingResponse Success(bool shown, OperationsPricingView? pricing)
    {
        return new GetOperationsPublicPricingResponse { Succeeded = true, Shown = shown, Pricing = pricing };
    }

    public static GetOperationsPublicPricingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPublicPricingResponse { Error = error };
    }
}
