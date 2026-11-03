namespace Vixly.Operations.Application.UseCases.Payables.ChangeOperationsBillSettings;

// =========================================================
// FILE: ChangeOperationsBillSettingsRequest.cs
//
// PURPOSE:
// Whether bills wait for someone else’s approval (Slice 8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsBillSettingsRequest
{
    public bool NeedApproval { get; init; }
}
