using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantCompanyProfile;

// =========================================================
// FILE: UpdateTenantCompanyProfileResponse.cs
//
// PURPOSE:
// The answer to a company profile update.
//
// RESPONSIBILITIES:
// - Say whether it was saved, or why not
//
// IMPORTANT NOTES:
// - Saving the same details again succeeds and changes nothing
// =========================================================
public sealed class UpdateTenantCompanyProfileResponse
{
    private UpdateTenantCompanyProfileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateTenantCompanyProfileResponse Success()
    {
        return new UpdateTenantCompanyProfileResponse { Succeeded = true };
    }

    public static UpdateTenantCompanyProfileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new UpdateTenantCompanyProfileResponse { Error = error };
    }
}
