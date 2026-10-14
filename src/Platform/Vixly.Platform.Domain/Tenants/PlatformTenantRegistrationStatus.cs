namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantRegistrationStatus.cs
//
// PURPOSE:
// Steps of a self-service registration.
//
// RESPONSIBILITIES:
// - Name the values the model accepts
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Slice 2 reaches PendingPlanSelection only (D-T4)
// - Account setup and first device moved out of Platform; the
//   values stay so V3 data maps one to one
// =========================================================
public enum PlatformTenantRegistrationStatus
{
    PendingEmailVerification = 0,
    PendingPlanSelection = 1,
    PendingPayment = 2,
    PendingAccountSetup = 3,
    PendingFirstDeviceRegistration = 4,
    Completed = 5,
    Cancelled = 6,
    Expired = 7
}
