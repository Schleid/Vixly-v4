using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Insurance.LinkOperationsCoverPolicy;

// =========================================================
// FILE: LinkOperationsCoverPolicyRequest.cs
//
// PURPOSE:
// Names the company policy a cover sold to customers sits under, or none (self-insured) (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class LinkOperationsCoverPolicyRequest
{
    public OperationsTransportMode Mode { get; init; }

    // Slice 5D: the product of that mode; none: the mode's first product.
    public Guid? PlanId { get; init; }

    // None: the company insures this cover itself.
    public Guid? PolicyId { get; init; }
}
