using Vixly.Operations.Domain.Arrivals;

namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsDiscrepancyRow.cs
//
// PURPOSE:
// Something that did not match at a destination (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsDiscrepancyRow(Guid Id, OperationsDiscrepancyKind Kind, OperationsDiscrepancyStatus Status, string Code, string? DispatchNumber, string WarehouseCode,
    DateTimeOffset CreatedUtc, string? Note, DateTimeOffset? ResolvedUtc, bool CanResolve);
