using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Legal;

// =========================================================
// FILE: OperationsLegalErrors.cs
//
// PURPOSE:
// Error codes of the Terms in Operations (Slice 1C).
//
// RESPONSIBILITIES:
// - Name every refusal of an acceptance
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText
// =========================================================
public static class OperationsLegalErrors
{
    public static Error VersionChanged { get; } = Error.Create("ops.legal.version_changed");

    public static Error NothingToAccept { get; } = Error.Create("ops.legal.nothing_to_accept");

    public static Error Refused { get; } = Error.Create("ops.legal.refused");
}
