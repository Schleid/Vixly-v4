using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformTenantLicensing;

// =========================================================
// FILE: GetPlatformTenantLicensingResponse.cs
//
// PURPOSE:
// Reads the Licensing section of a tenant page: seats, devices by state and actions (D-L15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantLicensingResponse
{
    private GetPlatformTenantLicensingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantLicensingSummary? Summary { get; private init; }

    public static GetPlatformTenantLicensingResponse Success(PlatformTenantLicensingSummary? summary)
    {
        return new GetPlatformTenantLicensingResponse { Succeeded = true, Summary = summary };
    }

    public static GetPlatformTenantLicensingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantLicensingResponse { Error = error };
    }
}
