namespace Vixly.Operations.Application.UseCases.Screens.RemoveOperationsCustomerScreen;

// =========================================================
// FILE: RemoveOperationsCustomerScreenRequest.cs
//
// PURPOSE:
// Removes a customer screen; it stops working at once (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsCustomerScreenRequest
{
    public Guid ScreenId { get; init; }
}
