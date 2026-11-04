using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.SaveOperationsPaymentSettings;

// =========================================================
// FILE: SaveOperationsPaymentSettingsResponse.cs
//
// PURPOSE:
// Change the payment methods the company accepts and its Zelle details (Slice 5F, C-1 and P-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsPaymentSettingsResponse
{
    private SaveOperationsPaymentSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsPaymentSettingsResponse Success()
    {
        return new SaveOperationsPaymentSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsPaymentSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsPaymentSettingsResponse { Error = error };
    }
}
