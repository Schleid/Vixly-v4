using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.ArchiveOperationsInsurancePolicy;

// =========================================================
// FILE: ArchiveOperationsInsurancePolicyResponse.cs
//
// PURPOSE:
// Archives an insurance policy the company no longer holds, or brings it back (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ArchiveOperationsInsurancePolicyResponse
{
    private ArchiveOperationsInsurancePolicyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ArchiveOperationsInsurancePolicyResponse Success()
    {
        return new ArchiveOperationsInsurancePolicyResponse { Succeeded = true };
    }

    public static ArchiveOperationsInsurancePolicyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ArchiveOperationsInsurancePolicyResponse { Error = error };
    }
}
