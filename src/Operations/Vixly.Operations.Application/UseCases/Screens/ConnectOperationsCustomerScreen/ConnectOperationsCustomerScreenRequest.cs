namespace Vixly.Operations.Application.UseCases.Screens.ConnectOperationsCustomerScreen;

// =========================================================
// FILE: ConnectOperationsCustomerScreenRequest.cs
//
// PURPOSE:
// The screen itself, with the code a clerk gave: it becomes that customer screen and receives its own secret (Slice 5F step 3, P-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ConnectOperationsCustomerScreenRequest
{
    public string? Code { get; init; }
}
