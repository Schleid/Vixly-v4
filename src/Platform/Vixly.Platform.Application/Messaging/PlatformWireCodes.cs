using System.Text;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformWireCodes.cs
//
// PURPOSE:
// Turns domain enum values into stable wire codes (Slice 9, D-K10).
//
// RESPONSIBILITIES:
// - Write PendingActivation as pending_activation
//
// IMPORTANT NOTES:
// - Wire codes never change once published; renaming an enum member needs a mapping here
// =========================================================
public static class PlatformWireCodes
{
    public static string Of<T>(T value)
        where T : struct, Enum
    {
        string name = value.ToString();
        StringBuilder text = new(name.Length + 8);

        for (int index = 0; index < name.Length; index++)
        {
            char letter = name[index];

            if (char.IsUpper(letter) && index > 0)
            {
                text.Append('_');
            }

            text.Append(char.ToLowerInvariant(letter));
        }

        return text.ToString();
    }

    public static string? Of<T>(T? value)
        where T : struct, Enum
    {
        return value is { } known ? Of(known) : null;
    }

    // Reads a wire code back; null for an unknown code (the caller refuses it).
    public static T? Parse<T>(string? code)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        string text = code.Trim();

        foreach (T value in Enum.GetValues<T>())
        {
            if (string.Equals(Of(value), text, StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
    }
}
