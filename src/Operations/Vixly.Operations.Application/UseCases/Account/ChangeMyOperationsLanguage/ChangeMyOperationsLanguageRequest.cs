namespace Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsLanguage;

// =========================================================
// FILE: ChangeMyOperationsLanguageRequest.cs
//
// PURPOSE:
// Saves the language of my pages and emails (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsLanguageRequest
{
    public string Language { get; init; } = "en";
}
