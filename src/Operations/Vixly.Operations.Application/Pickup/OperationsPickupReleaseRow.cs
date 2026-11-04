using Vixly.Operations.Domain.Pickup;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupReleaseRow.cs
//
// PURPOSE:
// A release at the pickup counter, with its proof (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPickupReleaseRow(Guid ReleaseId, OperationsPickupCollector Collector, string CollectorName, OperationsPickupIdKind? IdKind, string? IdLast,
    bool Signed, bool Photographed, bool CodeChecked, int ParcelCount, DateTimeOffset ReleasedUtc, string WarehouseCode);
