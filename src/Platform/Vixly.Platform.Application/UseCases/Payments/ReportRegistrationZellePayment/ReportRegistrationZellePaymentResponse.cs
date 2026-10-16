using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ReportRegistrationZellePayment;

// =========================================================
// FILE: ReportRegistrationZellePaymentResponse.cs
//
// PURPOSE:
// Takes a self-registering company's "I paid with Zelle" report for its first invoice.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReportRegistrationZellePaymentResponse
{
    private ReportRegistrationZellePaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReportRegistrationZellePaymentResponse Success()
    {
        return new ReportRegistrationZellePaymentResponse { Succeeded = true };
    }

    public static ReportRegistrationZellePaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReportRegistrationZellePaymentResponse { Error = error };
    }
}
