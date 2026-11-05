namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsStepUpAction.cs
//
// PURPOSE:
// A staff action kept while its code was asked (OperationsStepUpContinuation).
//
// RESPONSIBILITIES:
// - Carry where the action was sent and its fields, without its old form token
//
// IMPORTANT NOTES:
// - Shown only to the person whose session asked for it, once
// =========================================================
public sealed record OperationsStepUpAction(string Action, IReadOnlyList<(string Name, string Value)> Fields);
