namespace Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel;

// =========================================================
// FILE: AssignOperationsParcelRequest.cs
//
// PURPOSE:
// Gives an unidentified parcel to the customer who proved it is theirs (Slice 3B, owner Q-R1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AssignOperationsParcelRequest
{
    public Guid ParcelId { get; init; }

    public string CustomerCode { get; init; } = string.Empty;

    public string VerifiedBy { get; init; } = string.Empty;

    public string CommandKey { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
