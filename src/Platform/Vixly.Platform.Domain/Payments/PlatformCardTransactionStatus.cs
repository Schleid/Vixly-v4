namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformCardTransactionStatus.cs
//
// PURPOSE:
// Where a card transaction stands with the provider (D-P5).
//
// RESPONSIBILITIES:
// - Name the where a card transaction stands with the provider (D-P5)
//
// IMPORTANT NOTES:
// - Only a verified provider event moves a charge to Succeeded
// - Values are stored; never renumber
// =========================================================
public enum PlatformCardTransactionStatus
{
    Created = 1,
    Pending = 2,
    RequiresAction = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6
}
