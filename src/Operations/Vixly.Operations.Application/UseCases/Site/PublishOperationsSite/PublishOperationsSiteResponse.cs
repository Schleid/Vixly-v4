using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.PublishOperationsSite;

// =========================================================
// FILE: PublishOperationsSiteResponse.cs
//
// PURPOSE:
// Makes the draft the public site (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PublishOperationsSiteResponse
{
    private PublishOperationsSiteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Version { get; private init; }

    public static PublishOperationsSiteResponse Success(int version)
    {
        return new PublishOperationsSiteResponse { Succeeded = true, Version = version };
    }

    public static PublishOperationsSiteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PublishOperationsSiteResponse { Error = error };
    }
}
