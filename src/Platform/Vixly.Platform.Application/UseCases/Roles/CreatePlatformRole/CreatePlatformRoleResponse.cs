using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.CreatePlatformRole;

// =========================================================
// FILE: CreatePlatformRoleResponse.cs
//
// PURPOSE:
// Result of CreatePlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformRoleResponse
{
    private CreatePlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformRoleId { get; private init; }

    public static CreatePlatformRoleResponse Success(Guid platformRoleId)
    {
        return new CreatePlatformRoleResponse { Succeeded = true, PlatformRoleId = platformRoleId };
    }

    public static CreatePlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformRoleResponse { Error = error };
    }
}
