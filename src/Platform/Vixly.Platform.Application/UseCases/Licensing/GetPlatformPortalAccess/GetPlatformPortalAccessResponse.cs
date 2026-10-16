using Vixly.Platform.Application.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformPortalAccess;

// =========================================================
// FILE: GetPlatformPortalAccessResponse.cs
//
// PURPOSE:
// Tells Edge and Operations which tenant a portal slug belongs to, its status and access level (D-L10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformPortalAccessResponse
{
    private GetPlatformPortalAccessResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformPortalAccess? Access { get; private init; }

    public static GetPlatformPortalAccessResponse Success(PlatformPortalAccess? access)
    {
        return new GetPlatformPortalAccessResponse { Succeeded = true, Access = access };
    }

    public static GetPlatformPortalAccessResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformPortalAccessResponse { Error = error };
    }
}
