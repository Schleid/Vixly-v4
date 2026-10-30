namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsHouseWaybills;

// =========================================================
// FILE: GetOperationsHouseWaybillsRequest.cs
//
// PURPOSE:
// The house air waybills of a closed dispatch as one PDF, or of one of its shipments (Slice 6B step 2, D-6; owner, A-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsHouseWaybillsRequest
{
    public Guid DispatchId { get; init; }

    // None: every shipment on the dispatch.
    public Guid? ShipmentId { get; init; }

    // "Consolidation" words for goods without contents, in the current language.
    public string ContentsFallback { get; init; } = string.Empty;
}
