namespace Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee;

// =========================================================
// FILE: ChangeOperationsStorageFeeRequest.cs
//
// PURPOSE:
// Changes the days a package waits free of charge and the storage fee per day after them (Slice 4D step 3, Q-D3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsStorageFeeRequest
{
    public int FreeDays { get; init; }

    public decimal? FeePerDay { get; init; }
}
