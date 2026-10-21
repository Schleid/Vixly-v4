namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingVisibility.cs
//
// PURPOSE:
// Who may see a tracking event (ADR-0021 decision 5).
//
// RESPONSIBILITIES:
// - Internal for staff; Customer for the owner of the package too
//
// IMPORTANT NOTES:
// - Public tracking shows only the milestones and dates of customer events, never arguments or notes
// =========================================================
public enum OperationsTrackingVisibility
{
    Internal = 0,
    Customer = 1
}
