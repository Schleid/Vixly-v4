using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.LinkOperationsCoverPolicy;

// =========================================================
// FILE: LinkOperationsCoverPolicyResponse.cs
//
// PURPOSE:
// Names the company policy a cover sold to customers sits under, or none (self-insured) (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class LinkOperationsCoverPolicyResponse
{
    private LinkOperationsCoverPolicyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static LinkOperationsCoverPolicyResponse Success()
    {
        return new LinkOperationsCoverPolicyResponse { Succeeded = true };
    }

    public static LinkOperationsCoverPolicyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new LinkOperationsCoverPolicyResponse { Error = error };
    }
}
