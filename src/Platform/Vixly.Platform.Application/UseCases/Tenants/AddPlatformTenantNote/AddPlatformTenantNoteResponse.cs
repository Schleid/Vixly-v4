using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.AddPlatformTenantNote;

// =========================================================
// FILE: AddPlatformTenantNoteResponse.cs
//
// PURPOSE:
// Result of AddPlatformTenantNote.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddPlatformTenantNoteResponse
{
    private AddPlatformTenantNoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid NoteId { get; private init; }

    public static AddPlatformTenantNoteResponse Success(Guid noteId)
    {
        return new AddPlatformTenantNoteResponse { Succeeded = true, NoteId = noteId };
    }

    public static AddPlatformTenantNoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddPlatformTenantNoteResponse { Error = error };
    }
}
