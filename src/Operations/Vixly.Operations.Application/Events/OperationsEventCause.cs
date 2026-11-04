namespace Vixly.Operations.Application.Events;

// =========================================================
// FILE: OperationsEventCause.cs
//
// PURPOSE:
// A cause a person can choose for a delay or an exception (Slice 6C, E-3).
//
// RESPONSIBILITIES:
// - Carry the code and, for the company's own, its words
//
// IMPORTANT NOTES:
// - A standard cause is worded by ui.event_reason.<code>
// =========================================================
public sealed record OperationsEventCause(string Code, string? TextEn, string? TextFr, string? TextEs)
{
    public bool IsStandard => TextEn is null;
}
