using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsBrandingErrors.cs
//
// PURPOSE:
// Error codes of the tenant brand and public site (Slice 1, 1B).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code
//
// IMPORTANT NOTES:
// - Codes start ops.brand.
// =========================================================
public static class OperationsBrandingErrors
{
    public static Error DisplayNameRequired { get; } = Error.Create("ops.brand.display_name_required");

    public static Error DisplayNameTooLong { get; } = Error.Create("ops.brand.display_name_too_long", ("maxLength", OperationsBrandSettings.DisplayNameMaxLength));

    public static Error ColourInvalid { get; } = Error.Create("ops.brand.colour_invalid");

    public static Error PrimaryContrastTooLow { get; } = Error.Create("ops.brand.primary_contrast_too_low", ("minimum", "4.5"));

    public static Error AccentContrastTooLow { get; } = Error.Create("ops.brand.accent_contrast_too_low", ("minimum", "3"));

    public static Error LinkInvalid { get; } = Error.Create("ops.brand.link_invalid");

    public static Error TextKeyInvalid { get; } = Error.Create("ops.brand.text_key_invalid");

    public static Error TextTooLong { get; } = Error.Create("ops.brand.text_too_long");

    public static Error TextNotPlain { get; } = Error.Create("ops.brand.text_not_plain");

    public static Error LanguageInvalid { get; } = Error.Create("ops.brand.language_invalid");

    public static Error ImageTypeNotAllowed { get; } = Error.Create("ops.brand.image_type_not_allowed");

    public static Error ImageTooLarge { get; } = Error.Create("ops.brand.image_too_large");

    public static Error ImageSizeInvalid { get; } = Error.Create("ops.brand.image_size_invalid");

    public static Error ImageNotFound { get; } = Error.Create("ops.brand.image_not_found");
}
