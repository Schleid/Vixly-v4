using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.GetPlatformUser;

// =========================================================
// FILE: GetPlatformUserResponse.cs
//
// PURPOSE:
// Result of GetPlatformUser.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformUserResponse
{
    private GetPlatformUserResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformUserDetail? User { get; private init; }

    public static GetPlatformUserResponse Success(PlatformUserDetail? user)
    {
        return new GetPlatformUserResponse { Succeeded = true, User = user };
    }

    public static GetPlatformUserResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformUserResponse { Error = error };
    }
}
