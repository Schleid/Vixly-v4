namespace Vixly.Operations.Application.UseCases.Support.FindOperationsSupportCaller;

// =========================================================
// FILE: FindOperationsSupportCallerRequest.cs
//
// PURPOSE:
// Step 1 of a support call: the caller's full phone number names one account, briefly (owner, 2026-10-29, S-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FindOperationsSupportCallerRequest
{
    public string? Phone { get; init; }
}
