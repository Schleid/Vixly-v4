using Vixly.Operations.Domain.Screens;

namespace Vixly.Operations.Application.Screens;

// =========================================================
// FILE: OperationsScreenRow.cs
//
// PURPOSE:
// A customer screen as the clerk sees it (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Carry its name, warehouse, when last seen, and what it shows now
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsScreenRow(Guid ScreenId, string Name, Guid WarehouseId, string WarehouseName, bool Paired, DateTimeOffset? LastSeenUtc, OperationsScreenStep Step,
    Guid? InvoiceId, Guid? ShipmentId, DateTimeOffset? ZelleAcceptedUtc);
