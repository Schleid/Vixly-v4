namespace Vixly.Operations.Application.Setup;

// =========================================================
// FILE: OperationsSetupMessages.cs
//
// PURPOSE:
// Outbox message types of tenant setup (Slice 1).
//
// RESPONSIBILITIES:
// - Name the types
//
// IMPORTANT NOTES:
// - Type names end with the version
// =========================================================
public static class OperationsSetupMessages
{
    public const string OwnerInvitationRequested = "ops.setup.owner_invitation_requested.v1";
}
