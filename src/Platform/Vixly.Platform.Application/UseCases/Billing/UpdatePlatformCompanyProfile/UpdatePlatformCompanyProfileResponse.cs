using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.UpdatePlatformCompanyProfile;

// =========================================================
// FILE: UpdatePlatformCompanyProfileResponse.cs
//
// PURPOSE:
// Sets Vixly's company details printed on invoices (D-B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformCompanyProfileResponse
{
    private UpdatePlatformCompanyProfileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformCompanyProfileResponse Success()
    {
        return new UpdatePlatformCompanyProfileResponse { Succeeded = true };
    }

    public static UpdatePlatformCompanyProfileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformCompanyProfileResponse { Error = error };
    }
}
