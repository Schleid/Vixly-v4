using Vixly.Operations.Application.Screens;

namespace Vixly.Operations.Application.UseCases.Screens.ActOnOperationsScreen;

// =========================================================
// FILE: ActOnOperationsScreenRequest.cs
//
// PURPOSE:
// What the customer does on the customer screen (Slice 5F step 3, P-2, P-3, P-9): choose Zelle and accept its terms, pay by card on Stripe’s page, go back, or sign the declaration.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ActOnOperationsScreenRequest
{
    public string? Secret { get; init; }

    public OperationsScreenAction Action { get; init; }

    // The version the customer saw: an action on a screen that has changed since is refused (the clerk sent something else).
    public string? Version { get; init; }

    // Sign: the signature drawn on the screen, as a PNG.
    public byte[]? Signature { get; init; }

    // Sign: the customer ticked that they accept the declaration and, when insured, the insurance terms.
    public bool Accepted { get; init; }

    // Card: where Stripe sends the customer back (this screen).
    public string? ReturnUrl { get; init; }
}
