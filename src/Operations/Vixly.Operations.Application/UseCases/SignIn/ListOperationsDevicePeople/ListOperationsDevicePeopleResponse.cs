using Vixly.Operations.Application.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.ListOperationsDevicePeople;

// =========================================================
// FILE: ListOperationsDevicePeopleResponse.cs
//
// PURPOSE:
// The people the PIN screen of a licensed device offers (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsDevicePeopleResponse
{
    private ListOperationsDevicePeopleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsQuickPerson> People { get; private init; } = Array.Empty<OperationsQuickPerson>();

    public static ListOperationsDevicePeopleResponse Success(IReadOnlyList<OperationsQuickPerson> people)
    {
        return new ListOperationsDevicePeopleResponse { Succeeded = true, People = people };
    }

    public static ListOperationsDevicePeopleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsDevicePeopleResponse { Error = error };
    }
}
