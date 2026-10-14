namespace Vixly.Platform.Application.UseCases.MyAccount.ChangeMyLanguage;

// =========================================================
// FILE: ChangeMyLanguageRequest.cs
//
// PURPOSE:
// Input for ChangeMyLanguage.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A two-letter code: en, fr or es
// =========================================================
public sealed class ChangeMyLanguageRequest
{
    public string Language { get; init; } = string.Empty;
}
