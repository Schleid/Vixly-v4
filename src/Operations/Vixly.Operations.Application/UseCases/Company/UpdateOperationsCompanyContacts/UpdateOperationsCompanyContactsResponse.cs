using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyContacts;

// =========================================================
// FILE: UpdateOperationsCompanyContactsResponse.cs
//
// PURPOSE:
// Saves the company's billing and technical contacts, language and time zone at Vixly (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateOperationsCompanyContactsResponse
{
    private UpdateOperationsCompanyContactsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateOperationsCompanyContactsResponse Success()
    {
        return new UpdateOperationsCompanyContactsResponse { Succeeded = true };
    }

    public static UpdateOperationsCompanyContactsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateOperationsCompanyContactsResponse { Error = error };
    }
}
