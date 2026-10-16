using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegal;

// =========================================================
// FILE: GetPlatformTenantLegalResponse.cs
//
// PURPOSE:
// Reads the Legal section of a tenant page: status, acceptances, reminders and actions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantLegalResponse
{
    private GetPlatformTenantLegalResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantLegalDetail? Detail { get; private init; }

    public static GetPlatformTenantLegalResponse Success(PlatformTenantLegalDetail? detail)
    {
        return new GetPlatformTenantLegalResponse { Succeeded = true, Detail = detail };
    }

    public static GetPlatformTenantLegalResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantLegalResponse { Error = error };
    }
}
