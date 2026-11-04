namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformOwnerSupportStatus.cs
//
// PURPOSE:
// Where an owner support request stands (D-O13).
//
// RESPONSIBILITIES:
// - Name each state of a support request
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformOwnerSupportStatus
{
    Pending = 1,
    Approved = 2,
    Refused = 3,
    Completed = 4
}
