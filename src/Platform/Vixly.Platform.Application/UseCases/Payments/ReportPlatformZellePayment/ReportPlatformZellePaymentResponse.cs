using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ReportPlatformZellePayment;

// =========================================================
// FILE: ReportPlatformZellePaymentResponse.cs
//
// PURPOSE:
// Takes a customer's "I paid with Zelle" report from a pay link; staff check it before anything is paid.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReportPlatformZellePaymentResponse
{
    private ReportPlatformZellePaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReportPlatformZellePaymentResponse Success()
    {
        return new ReportPlatformZellePaymentResponse { Succeeded = true };
    }

    public static ReportPlatformZellePaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReportPlatformZellePaymentResponse { Error = error };
    }
}
