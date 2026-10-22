using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyLabels;

// =========================================================
// FILE: GetOperationsSupplyLabelsRequest.cs
//
// PURPOSE:
// A sheet of shelf labels for a supply: its name, its item code as a barcode and its price today (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSupplyLabelsRequest
{
    public Guid SupplyId { get; init; }

    // The labels on the sheet, 1 to 30 (a 3 by 10 sheet).
    public int Count { get; init; } = 30;
}
