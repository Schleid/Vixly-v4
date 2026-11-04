using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyLimits.cs
//
// PURPOSE:
// The sizes and counts the supplies catalogue accepts (Slice 5E).
//
// RESPONSIBILITIES:
// - Bound every text and list of a supply
// - Read an item code or a barcode the same way everywhere
//
// IMPORTANT NOTES:
// - A code or barcode is kept in capitals: a scan matches what was typed whatever the case
// - Barcodes: letters, digits and - . / (EAN, UPC and Code 128 as scanners send them)
// =========================================================
public static class OperationsSupplyLimits
{
    public const int CodeMaxLength = 30;

    public const int NameMaxLength = 80;

    public const int UnitMaxLength = 30;

    public const int BarcodeMaxLength = 48;

    public const int MaxBarcodes = 5;

    public const int MaxBreaks = 5;

    public const int MaxWarehousePrices = 50;

    public const int MaxSupplies = 2000;

    public const int MaxQuantity = 9999;

    // A scan read as a code, or null when the text cannot be one (a name with spaces, for instance): never throws.
    public static string? TryCode(string? value, int maxLength)
    {
        string? code = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
        return code is not null && code.Length <= maxLength && code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '/') ? code : null;
    }

    // An item code or a barcode: trimmed, in capitals; null when empty.
    public static string? Code(string? value, int maxLength, Error error)
    {
        string? code = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        return code is null || (code.Length <= maxLength && code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '/'))
            ? code
            : throw new DomainException(error);
    }
}
