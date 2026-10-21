namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteServices.cs
//
// PURPOSE:
// The services a tenant shows on its site (Slice 1B).
//
// RESPONSIBILITIES:
// - Combine the service cards
//
// IMPORTANT NOTES:
// - Flags, stored as int
// =========================================================
[Flags]
public enum OperationsSiteServices
{
    None = 0,
    Air = 1,
    Ocean = 2,
    Consolidation = 4,
    Mailbox = 8,
    DoorToDoor = 16,
    Pickup = 32,
    All = Air | Ocean | Consolidation | Mailbox | DoorToDoor | Pickup
}
