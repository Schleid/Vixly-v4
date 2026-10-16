using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.OpenPlatformPaymentLink;

// =========================================================
// FILE: OpenPlatformPaymentLinkResponse.cs
//
// PURPOSE:
// Shows what a pay or save-card link is for, before the tenant continues to the provider.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenPlatformPaymentLinkResponse
{
    private OpenPlatformPaymentLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformPaymentLinkSummary? Link { get; private init; }

    public static OpenPlatformPaymentLinkResponse Success(PlatformPaymentLinkSummary? link)
    {
        return new OpenPlatformPaymentLinkResponse { Succeeded = true, Link = link };
    }

    public static OpenPlatformPaymentLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenPlatformPaymentLinkResponse { Error = error };
    }
}
