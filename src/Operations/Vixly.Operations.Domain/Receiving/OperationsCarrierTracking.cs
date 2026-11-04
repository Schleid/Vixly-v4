using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsCarrierTracking.cs
//
// PURPOSE:
// Normalizes a carrier tracking number (Slice 3B).
//
// RESPONSIBILITIES:
// - Upper case, without spaces or dashes, letters and digits only
//
// IMPORTANT NOTES:
// - The same package typed with or without spaces matches its pre-alert
// =========================================================
public static class OperationsCarrierTracking
{
    private static readonly Regex Allowed = new("^[A-Z0-9]{5,40}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Normalize(string? value)
    {
        string text = new((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character) && character != '-').ToArray());
        text = text.ToUpperInvariant();
        return Allowed.IsMatch(text) ? text : throw new DomainException(OperationsReceivingErrors.TrackingNumberInvalid);
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        try
        {
            normalized = Normalize(value);
            return true;
        }
        catch (DomainException)
        {
            normalized = string.Empty;
            return false;
        }
    }
}
