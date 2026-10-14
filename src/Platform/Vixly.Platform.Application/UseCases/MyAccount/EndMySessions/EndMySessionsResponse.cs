using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.EndMySessions;

// =========================================================
// FILE: EndMySessionsResponse.cs
//
// PURPOSE:
// Result of EndMySessions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndMySessionsResponse
{
    private EndMySessionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int EndedCount { get; private init; }

    public static EndMySessionsResponse Success(int endedCount)
    {
        return new EndMySessionsResponse { Succeeded = true, EndedCount = endedCount };
    }

    public static EndMySessionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndMySessionsResponse { Error = error };
    }
}
