using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.EndPlatformUserSessions;

// =========================================================
// FILE: EndPlatformUserSessionsResponse.cs
//
// PURPOSE:
// Result of EndPlatformUserSessions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndPlatformUserSessionsResponse
{
    private EndPlatformUserSessionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int EndedCount { get; private init; }

    public static EndPlatformUserSessionsResponse Success(int endedCount)
    {
        return new EndPlatformUserSessionsResponse { Succeeded = true, EndedCount = endedCount };
    }

    public static EndPlatformUserSessionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndPlatformUserSessionsResponse { Error = error };
    }
}
