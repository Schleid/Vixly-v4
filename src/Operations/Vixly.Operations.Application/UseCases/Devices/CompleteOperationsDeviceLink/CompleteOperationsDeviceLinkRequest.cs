namespace Vixly.Operations.Application.UseCases.Devices.CompleteOperationsDeviceLink;

// =========================================================
// FILE: CompleteOperationsDeviceLinkRequest.cs
//
// PURPOSE:
// Completes an emailed device link (first device or owner recovery) on the computer that opened it (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompleteOperationsDeviceLinkRequest
{
    public string Token { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Kind { get; init; } = "pc";
}
