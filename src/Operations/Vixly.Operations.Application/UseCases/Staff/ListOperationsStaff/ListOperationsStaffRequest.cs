namespace Vixly.Operations.Application.UseCases.Staff.ListOperationsStaff;

// =========================================================
// FILE: ListOperationsStaffRequest.cs
//
// PURPOSE:
// Lists the staff of the tenant, by page (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsStaffRequest
{
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
}
