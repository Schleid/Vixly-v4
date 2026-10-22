using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentSettings;

// =========================================================
// FILE: GetOperationsPaymentSettingsResponse.cs
//
// PURPOSE:
// Settings → Payments (Slice 5F, C-1): the methods the company accepts and its Zelle details.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPaymentSettingsResponse
{
    private GetOperationsPaymentSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsTenantSettings Settings { get; private init; } = null!;

    public bool CanEdit { get; private init; }

    public static GetOperationsPaymentSettingsResponse Success(Vixly.Operations.Domain.Settings.OperationsTenantSettings settings, bool canEdit)
    {
        return new GetOperationsPaymentSettingsResponse { Succeeded = true, Settings = settings, CanEdit = canEdit };
    }

    public static GetOperationsPaymentSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPaymentSettingsResponse { Error = error };
    }
}
