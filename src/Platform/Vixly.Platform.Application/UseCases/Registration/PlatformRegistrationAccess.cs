using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration;

// =========================================================
// FILE: PlatformRegistrationAccess.cs
//
// PURPOSE:
// The registration an access token opens, or why it does not.
//
// RESPONSIBILITIES:
// - Carry the registration or the refusal
// - Say whether reading it just expired it, so the caller saves
//
// IMPORTANT NOTES:
// - Built only by PlatformRegistrationLinks
// =========================================================
public sealed class PlatformRegistrationAccess
{
    private PlatformRegistrationAccess(PlatformTenantRegistration? registration, Error? error, bool expiredNow)
    {
        Registration = registration;
        Error = error;
        ExpiredNow = expiredNow;
    }

    public PlatformTenantRegistration? Registration { get; }

    public Error? Error { get; }

    public bool ExpiredNow { get; }

    public static PlatformRegistrationAccess Granted(PlatformTenantRegistration registration)
    {
        return new PlatformRegistrationAccess(registration, null, false);
    }

    public static PlatformRegistrationAccess Refused(Error error, bool expiredNow = false)
    {
        return new PlatformRegistrationAccess(null, error, expiredNow);
    }
}
