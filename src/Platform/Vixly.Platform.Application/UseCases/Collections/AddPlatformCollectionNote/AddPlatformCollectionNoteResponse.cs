using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.AddPlatformCollectionNote;

// =========================================================
// FILE: AddPlatformCollectionNoteResponse.cs
//
// PURPOSE:
// Adds an internal note to a collections case (D-C8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddPlatformCollectionNoteResponse
{
    private AddPlatformCollectionNoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AddPlatformCollectionNoteResponse Success()
    {
        return new AddPlatformCollectionNoteResponse { Succeeded = true };
    }

    public static AddPlatformCollectionNoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddPlatformCollectionNoteResponse { Error = error };
    }
}
