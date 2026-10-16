using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.ListPlatformTaxRates;

// =========================================================
// FILE: ListPlatformTaxRatesResponse.cs
//
// PURPOSE:
// Lists tax rates by country and region, newest first (D-B1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformTaxRatesResponse
{
    private ListPlatformTaxRatesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformTaxRateItem> Rates { get; private init; } = Array.Empty<PlatformTaxRateItem>();

    public bool CanEdit { get; private init; }

    public static ListPlatformTaxRatesResponse Success(IReadOnlyList<PlatformTaxRateItem> rates, bool canEdit)
    {
        return new ListPlatformTaxRatesResponse { Succeeded = true, Rates = rates, CanEdit = canEdit };
    }

    public static ListPlatformTaxRatesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformTaxRatesResponse { Error = error };
    }
}
