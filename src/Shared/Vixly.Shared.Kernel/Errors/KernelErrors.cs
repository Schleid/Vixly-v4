namespace Vixly.Shared.Kernel.Errors;

// =========================================================
// FILE: KernelErrors.cs
//
// PURPOSE:
// The catalogue of errors raised by Kernel types.
//
// RESPONSIBILITIES:
// - Declare every Kernel error code in one place
//
// IMPORTANT NOTES:
// - Codes start with shared. (Global Standard 4.2)
// - Each code needs en, fr and es text in
//   Vixly.Shared.Localization resources; the architecture
//   tests enforce this once that project exists
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class KernelErrors
{
    public static Error EmailInvalid { get; } =
        Error.Create("shared.email.invalid");

    public static Error PhoneInvalid { get; } =
        Error.Create("shared.phone.invalid");

    public static Error LanguageUnsupported { get; } =
        Error.Create("shared.language.unsupported");

    public static Error TimeMustBeUtc { get; } =
        Error.Create("shared.time.must_be_utc");
}
