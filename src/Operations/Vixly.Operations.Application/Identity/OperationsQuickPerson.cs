namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsQuickPerson.cs
//
// PURPOSE:
// A person the PIN screen of a licensed device offers (Slice 12A).
//
// RESPONSIBILITIES:
// - Name the person and say whether their PIN is locked
//
// IMPORTANT NOTES:
// - Only people with a PIN who signed in on the device in the last 30 days
// =========================================================
public sealed record OperationsQuickPerson(Guid UserId, string Name, bool Locked);
