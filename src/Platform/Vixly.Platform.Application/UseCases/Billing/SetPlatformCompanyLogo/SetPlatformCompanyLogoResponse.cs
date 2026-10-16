using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.SetPlatformCompanyLogo;

// =========================================================
// FILE: SetPlatformCompanyLogoResponse.cs
//
// PURPOSE:
// Sets or removes the logo printed on invoices (PNG or JPEG, D-B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformCompanyLogoResponse
{
    private SetPlatformCompanyLogoResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformCompanyLogoResponse Success()
    {
        return new SetPlatformCompanyLogoResponse { Succeeded = true };
    }

    public static SetPlatformCompanyLogoResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformCompanyLogoResponse { Error = error };
    }
}
