namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsPasswordCheck.cs
//
// PURPOSE:
// The result of checking a password against its hash.
//
// RESPONSIBILITIES:
// - Name the outcomes
//
// IMPORTANT NOTES:
// - NeedsRehash: correct, but hashed with an older cost (M-4)
// =========================================================
public enum OperationsPasswordCheck
{
    Failed = 0,
    Succeeded = 1,
    SucceededNeedsRehash = 2
}
