namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformTermsAcceptanceSource.cs
//
// PURPOSE:
// How a tenant's acceptance reached Vixly.
//
// RESPONSIBILITIES:
// - Registration, the owner in Operations, or a staff record of a signed paper
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformTermsAcceptanceSource
{
    Registration = 1,
    Operations = 2,
    StaffRecord = 3
}
