using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.CreateOperationsPayLink;

// =========================================================
// FILE: CreateOperationsPayLinkResponse.cs
//
// PURPOSE:
// Asks Vixly for a link to pay one invoice online (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsPayLinkResponse
{
    private CreateOperationsPayLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Url { get; private init; } = string.Empty;

    public static CreateOperationsPayLinkResponse Success(string url)
    {
        return new CreateOperationsPayLinkResponse { Succeeded = true, Url = url };
    }

    public static CreateOperationsPayLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsPayLinkResponse { Error = error };
    }
}
