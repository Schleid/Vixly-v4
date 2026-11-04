namespace Vixly.Operations.Application.UseCases.Site.GetOperationsPublicPricing;

// =========================================================
// FILE: GetOperationsPublicPricingRequest.cs
//
// PURPOSE:
// The company's published prices on its public website (owner, 2026-10-21): the whole list on the Pricing page, only a yes or a no for the menu.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPublicPricingRequest
{
    public bool Full { get; init; }
}
