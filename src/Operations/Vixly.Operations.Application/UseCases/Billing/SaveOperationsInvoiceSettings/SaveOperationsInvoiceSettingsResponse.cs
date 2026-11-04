using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.SaveOperationsInvoiceSettings;

// =========================================================
// FILE: SaveOperationsInvoiceSettingsResponse.cs
//
// PURPOSE:
// Change the company's invoice settings (Slice 5F, F-5, I-4 and I-10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsInvoiceSettingsResponse
{
    private SaveOperationsInvoiceSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsInvoiceSettingsResponse Success()
    {
        return new SaveOperationsInvoiceSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsInvoiceSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsInvoiceSettingsResponse { Error = error };
    }
}
