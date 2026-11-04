namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsReviewNameStyle.cs
//
// PURPOSE:
// How a customer's name appears with their review (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Name the two choices: first name and initial, or first name only
//
// IMPORTANT NOTES:
// - Stored as a number; the full name is never shown
// =========================================================
public enum OperationsReviewNameStyle
{
    FirstAndInitial = 0,
    FirstOnly = 1
}
