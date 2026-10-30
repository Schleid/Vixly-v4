namespace Vixly.Operations.Domain.Pickup;

// =========================================================
// FILE: OperationsPickupIdKind.cs
//
// PURPOSE:
// The identity document shown at the pickup counter (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// - Only its last digits are kept, never the full number
// =========================================================
public enum OperationsPickupIdKind
{
    NationalId = 1,
    Passport = 2,
    DriverLicence = 3,
    Other = 4
}
