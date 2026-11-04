namespace Vixly.Operations.Application.UseCases.Devices.EnrolOperationsDevice;

// =========================================================
// FILE: EnrolOperationsDeviceRequest.cs
//
// PURPOSE:
// Asks to add the computer a signed-in staff member is using; an Owner approves it (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EnrolOperationsDeviceRequest
{
    public string Name { get; init; } = string.Empty;

    public string Kind { get; init; } = "pc";
}
