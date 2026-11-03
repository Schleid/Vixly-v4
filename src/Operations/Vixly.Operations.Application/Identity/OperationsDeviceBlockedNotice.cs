namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsDeviceBlockedNotice.cs
//
// PURPOSE:
// The outbox message that tells a company's administrators a device was blocked (owner, R-6).
//
// RESPONSIBILITIES:
// - Carry the block and the company's address for the link
//
// IMPORTANT NOTES:
// - Written in the same commit as the block, so the notice is never lost or sent for a block that was not kept
// =========================================================
public sealed record OperationsDeviceBlockedNotice(Guid BlockId, string? PortalSlug)
{
    public const string MessageType = "ops.devices.blocked.v1";
}
