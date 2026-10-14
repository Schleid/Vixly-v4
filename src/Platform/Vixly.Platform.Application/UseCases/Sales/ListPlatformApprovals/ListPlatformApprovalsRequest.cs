namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformApprovals;

// =========================================================
// FILE: ListPlatformApprovalsRequest.cs
//
// PURPOSE:
// Lists what waits for the current staff member's approval (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformApprovalsRequest
{
    public string? Language { get; init; }
}
