namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformDisputeStatus.cs
//
// PURPOSE:
// Where a bank dispute on a card charge stands (D-P15).
//
// RESPONSIBILITIES:
// - Name the where a bank dispute on a card charge stands (D-P15)
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformDisputeStatus
{
    Open = 1,
    Won = 2,
    Lost = 3
}
