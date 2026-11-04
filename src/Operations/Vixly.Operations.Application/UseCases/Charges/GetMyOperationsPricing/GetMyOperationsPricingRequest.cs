namespace Vixly.Operations.Application.UseCases.Charges.GetMyOperationsPricing;

// =========================================================
// FILE: GetMyOperationsPricingRequest.cs
//
// PURPOSE:
// The prices a signed-in customer sees on the portal's Pricing page (owner, 2026-10-21): the regular prices and their own.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsPricingRequest
{
    public string? SessionToken { get; init; }
}
