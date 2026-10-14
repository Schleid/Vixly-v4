using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.PreviewPlatformPlanPrice;

// =========================================================
// FILE: PreviewPlatformPlanPriceResponse.cs
//
// PURPOSE:
// Prices a period of a plan for a term and a number of licences (D-S9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PreviewPlatformPlanPriceResponse
{
    private PreviewPlatformPlanPriceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public decimal PeriodAmount { get; private init; }

    public decimal SetupFee { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static PreviewPlatformPlanPriceResponse Success(decimal periodAmount, decimal setupFee, string currencyCode)
    {
        return new PreviewPlatformPlanPriceResponse { Succeeded = true, PeriodAmount = periodAmount, SetupFee = setupFee, CurrencyCode = currencyCode };
    }

    public static PreviewPlatformPlanPriceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PreviewPlatformPlanPriceResponse { Error = error };
    }
}
