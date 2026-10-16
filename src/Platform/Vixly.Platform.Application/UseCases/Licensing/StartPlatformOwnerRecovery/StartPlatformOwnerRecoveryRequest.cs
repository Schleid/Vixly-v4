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
}
