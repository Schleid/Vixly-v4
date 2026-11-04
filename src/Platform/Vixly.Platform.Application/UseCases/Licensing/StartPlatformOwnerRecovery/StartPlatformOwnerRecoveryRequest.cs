namespace Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecovery;

// =========================================================
// FILE: StartPlatformOwnerRecoveryRequest.cs
//
// PURPOSE:
// Vixly staff email the owner a device recovery link (D-L8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartPlatformOwnerRecoveryRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;

    // How support confirmed the caller is the Owner (D-O13).
    public string IdentityCheck { get; init; } = string.Empty;

    // The lost device that gives its seat when none is free.
    public Guid? ReplaceDeviceId { get; init; }
}
