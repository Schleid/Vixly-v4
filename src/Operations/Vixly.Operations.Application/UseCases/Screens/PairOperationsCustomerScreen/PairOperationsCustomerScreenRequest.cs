namespace Vixly.Operations.Application.UseCases.Screens.PairOperationsCustomerScreen;

// =========================================================
// FILE: PairOperationsCustomerScreenRequest.cs
//
// PURPOSE:
// Adds a customer screen to a warehouse, or gives an existing one a new code, and returns the code to type on the screen (Slice 5F step 3, P-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PairOperationsCustomerScreenRequest
{
    // An existing screen to pair again (a new tablet); otherwise a new screen at this warehouse with this name.
    public Guid? ScreenId { get; init; }

    public Guid WarehouseId { get; init; }

    public string? Name { get; init; }
}
