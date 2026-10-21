using Vixly.Operations.Domain.Branding;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsSiteDraft;

// =========================================================
// FILE: GetOperationsSiteDraftResponse.cs
//
// PURPOSE:
// The draft site for the Owner editor (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSiteDraftResponse
{
    private GetOperationsSiteDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsBrandSettings? Draft { get; private init; }

    public IReadOnlyDictionary<string, string> Texts { get; private init; } = new Dictionary<string, string>();

    public int PublishedVersion { get; private init; }

    public bool HasUnpublishedChanges { get; private init; }

    public static GetOperationsSiteDraftResponse Success(OperationsBrandSettings? draft, IReadOnlyDictionary<string, string> texts, int publishedVersion, bool hasUnpublishedChanges)
    {
        return new GetOperationsSiteDraftResponse { Succeeded = true, Draft = draft, Texts = texts, PublishedVersion = publishedVersion, HasUnpublishedChanges = hasUnpublishedChanges };
    }

    public static GetOperationsSiteDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSiteDraftResponse { Error = error };
    }
}
