using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionCase;

// =========================================================
// FILE: GetPlatformCollectionCaseResponse.cs
//
// PURPOSE:
// Reads a collections case: its overdue invoices, history and the actions the reader may take.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformCollectionCaseResponse
{
    private GetPlatformCollectionCaseResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformCollectionCaseDetail? Detail { get; private init; }

    public static GetPlatformCollectionCaseResponse Success(PlatformCollectionCaseDetail? detail)
    {
        return new GetPlatformCollectionCaseResponse { Succeeded = true, Detail = detail };
    }

    public static GetPlatformCollectionCaseResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformCollectionCaseResponse { Error = error };
    }
}
