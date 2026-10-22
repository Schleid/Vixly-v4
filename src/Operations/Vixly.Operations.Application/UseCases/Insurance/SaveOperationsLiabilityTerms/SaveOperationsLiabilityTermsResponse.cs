using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsLiabilityTerms;

// =========================================================
// FILE: SaveOperationsLiabilityTermsResponse.cs
//
// PURPOSE:
// Sets what the company owes, for one transport mode, when the customer did not insure (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsLiabilityTermsResponse
{
    private SaveOperationsLiabilityTermsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsLiabilityTermsResponse Success()
    {
        return new SaveOperationsLiabilityTermsResponse { Succeeded = true };
    }

    public static SaveOperationsLiabilityTermsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsLiabilityTermsResponse { Error = error };
    }
}
