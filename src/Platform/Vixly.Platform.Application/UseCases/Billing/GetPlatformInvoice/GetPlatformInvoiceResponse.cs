using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformInvoice;

// =========================================================
// FILE: GetPlatformInvoiceResponse.cs
//
// PURPOSE:
// Reads one invoice with its lines, payments, credit notes and history.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformInvoiceResponse
{
    private GetPlatformInvoiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformInvoiceDetail? Invoice { get; private init; }

    public PlatformInvoiceAbilities Abilities { get; private init; } = PlatformInvoiceAbilities.None;

    public static GetPlatformInvoiceResponse Success(PlatformInvoiceDetail? invoice, PlatformInvoiceAbilities abilities)
    {
        return new GetPlatformInvoiceResponse { Succeeded = true, Invoice = invoice, Abilities = abilities };
    }

    public static GetPlatformInvoiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformInvoiceResponse { Error = error };
    }
}
