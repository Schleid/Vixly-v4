using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.AddPlatformTaxRate;

// =========================================================
// FILE: AddPlatformTaxRateResponse.cs
//
// PURPOSE:
// Adds a tax rate for a country or region from a date; earlier rates stay for issued invoices (D-B1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddPlatformTaxRateResponse
{
    private AddPlatformTaxRateResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AddPlatformTaxRateResponse Success()
    {
        return new AddPlatformTaxRateResponse { Succeeded = true };
    }

    public static AddPlatformTaxRateResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddPlatformTaxRateResponse { Error = error };
    }
}
