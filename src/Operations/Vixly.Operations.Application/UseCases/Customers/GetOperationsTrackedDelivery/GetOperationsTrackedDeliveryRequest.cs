namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsTrackedDelivery;

// =========================================================
// FILE: GetOperationsTrackedDeliveryRequest.cs
//
// PURPOSE:
// What the private link of an "out for delivery" notice shows (Slice 12C, owner H-5), without signing in.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsTrackedDeliveryRequest
{
    public string Token { get; init; } = string.Empty;
}
