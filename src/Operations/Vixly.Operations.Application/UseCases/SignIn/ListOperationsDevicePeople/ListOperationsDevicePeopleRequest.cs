namespace Vixly.Operations.Application.UseCases.SignIn.ListOperationsDevicePeople;

// =========================================================
// FILE: ListOperationsDevicePeopleRequest.cs
//
// PURPOSE:
// The people the PIN screen of a licensed device offers (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsDevicePeopleRequest
{
    public Guid? DeviceId { get; init; }

    public string? DeviceSecret { get; init; }
}
