namespace Vixly.Operations.Application.UseCases.Items.GetOperationsParcelItems;

// =========================================================
// FILE: GetOperationsParcelItemsRequest.cs
//
// PURPOSE:
// The items of a parcel, for staff to classify for customs (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsParcelItemsRequest
{
    public Guid ParcelId { get; init; }
}
