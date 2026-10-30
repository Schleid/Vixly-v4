using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsArrivalRow.cs
//
// PURPOSE:
// A dispatch on its way to a destination warehouse, or received there (Slice 7).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsArrivalRow(Guid DispatchId, string Number, string FromCode, OperationsDispatchStatus Status, DateTimeOffset? DepartedUtc, DateTimeOffset? ArrivedUtc,
    int BundleCount, bool Onward, DateTimeOffset? ReceivedUtc);
