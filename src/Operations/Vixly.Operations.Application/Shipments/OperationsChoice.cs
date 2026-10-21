namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsChoice.cs
//
// PURPOSE:
// One choice offered on a customer form: a mailbox warehouse or a destination branch (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Name the choice and say where it is
//
// IMPORTANT NOTES:
// - Only active places the customer may use
// =========================================================
public sealed record OperationsChoice(Guid Id, string Name, string? Detail);
