using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;

// =========================================================
// FILE: GetMyPlatformAccessResponse.cs
//
// PURPOSE:
// Result of GetMyPlatformAccess.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - For showing and hiding menu items and buttons only; never
//   authorization (S-1)
// =========================================================
public sealed class GetMyPlatformAccessResponse
{
    private GetMyPlatformAccessResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyCollection<string> PermissionCodes { get; private init; } = Array.Empty<string>();

    public bool IsSuperUser { get; private init; }

    public static GetMyPlatformAccessResponse Success(IReadOnlyCollection<string> permissionCodes, bool isSuperUser)
    {
        return new GetMyPlatformAccessResponse { Succeeded = true, PermissionCodes = permissionCodes, IsSuperUser = isSuperUser };
    }

    public static GetMyPlatformAccessResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyPlatformAccessResponse { Error = error };
    }
}
