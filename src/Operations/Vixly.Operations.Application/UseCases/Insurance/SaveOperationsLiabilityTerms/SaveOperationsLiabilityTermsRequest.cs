using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsLiabilityTerms;

// =========================================================
// FILE: SaveOperationsLiabilityTermsRequest.cs
//
// PURPOSE:
// Sets what the company owes, for one transport mode, when the customer did not insure (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsLiabilityTermsRequest
{
    public OperationsTransportMode Mode { get; init; }

    public OperationsLiabilityBasis Basis { get; init; } = OperationsLiabilityBasis.None;

    public decimal? Amount { get; init; }

    public decimal? Cap { get; init; }

    public string? CustomerText { get; init; }
}
