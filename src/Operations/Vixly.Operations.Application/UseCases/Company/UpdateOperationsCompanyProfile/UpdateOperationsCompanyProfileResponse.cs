using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyProfile;

// =========================================================
// FILE: UpdateOperationsCompanyProfileResponse.cs
//
// PURPOSE:
// The answer to a company profile update.
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateOperationsCompanyProfileResponse
{
    private UpdateOperationsCompanyProfileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateOperationsCompanyProfileResponse Success()
    {
        return new UpdateOperationsCompanyProfileResponse { Succeeded = true };
    }

    public static UpdateOperationsCompanyProfileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateOperationsCompanyProfileResponse { Error = error };
    }
}
