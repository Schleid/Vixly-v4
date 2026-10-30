using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchText.cs
//
// PURPOSE:
// Checks the free text of carriers and dispatches (Slice 6B).
//
// RESPONSIBILITIES:
// - Trim, bound and refuse control characters and markup
//
// IMPORTANT NOTES:
// - Empty is none
// =========================================================
public static class OperationsDispatchText
{
    public static string? Optional(string? value, int max)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null || (text.Length <= max && !text.Any(char.IsControl) && text.IndexOfAny(['<', '>']) < 0)
            ? text
            : throw new DomainException(OperationsDispatchErrors.TextInvalid);
    }

    public static string Required(string? value, int max, Vixly.Shared.Kernel.Errors.Error missing) =>
        Optional(value, max) ?? throw new DomainException(missing);
}
