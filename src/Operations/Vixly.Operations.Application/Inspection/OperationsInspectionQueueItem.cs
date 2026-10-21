using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Inspection;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsInspectionQueueItem.cs
//
// PURPOSE:
// One line of the inspection queue or of the disposals awaiting approval (Slice 3C).
//
// RESPONSIBILITIES:
// - Carry the inspection with the parcel it is about
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsInspectionQueueItem(OperationsInspection Inspection, OperationsParcelSummary Parcel);
