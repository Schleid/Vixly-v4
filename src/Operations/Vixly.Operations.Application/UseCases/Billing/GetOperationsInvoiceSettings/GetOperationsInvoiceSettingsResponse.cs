using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceSettings;

// =========================================================
// FILE: GetOperationsInvoiceSettingsResponse.cs
//
// PURPOSE:
// The company's invoice settings (Slice 5F, I-10): the tax name and number, the rates, how taxes show and the note.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInvoiceSettingsResponse
{
    private GetOperationsInvoiceSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTenantSettings Settings { get; private init; } = null!;

    public bool CanEdit { get; private init; }

    public static GetOperationsInvoiceSettingsResponse Success(OperationsTenantSettings settings, bool canEdit)
    {
        return new GetOperationsInvoiceSettingsResponse { Succeeded = true, Settings = settings, CanEdit = canEdit };
    }

    public static GetOperationsInvoiceSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInvoiceSettingsResponse { Error = error };
    }
}
