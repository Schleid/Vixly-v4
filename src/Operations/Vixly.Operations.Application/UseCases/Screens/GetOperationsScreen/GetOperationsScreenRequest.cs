namespace Vixly.Operations.Application.UseCases.Screens.GetOperationsScreen;

// =========================================================
// FILE: GetOperationsScreenRequest.cs
//
// PURPOSE:
// What a customer screen shows now (Slice 5F step 3, P-2, P-3, P-9): the invoice and the methods, the Zelle steps, the card, or the declaration to sign.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsScreenRequest
{
    // The screen's own secret, from its cookie.
    public string? Secret { get; init; }

    // Only the version (asked every few seconds): the page reloads when it changes.
    public bool VersionOnly { get; init; }
}
