namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsCommandKey.cs
//
// PURPOSE:
// Checks the reference a form or scanner sends with a command (ADR-0019, ADR-0021 decision 4).
//
// RESPONSIBILITIES:
// - Accept 8 to 60 letters, digits or dashes
//
// IMPORTANT NOTES:
// - The reference makes a repeated command return its first result
// =========================================================
public static class OperationsCommandKey
{
    public static bool IsValid(string? key) =>
        key is { Length: >= 8 and <= 60 } && key.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
}
