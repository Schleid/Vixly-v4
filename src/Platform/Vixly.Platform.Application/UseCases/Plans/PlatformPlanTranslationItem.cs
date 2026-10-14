namespace Vixly.Platform.Application.UseCases.Plans;

// =========================================================
// FILE: PlatformPlanTranslationItem.cs
//
// PURPOSE:
// A plan's name and description in one language.
//
// RESPONSIBILITIES:
// - Carry the language code, name and description
//
// IMPORTANT NOTES:
// - English is always present
// =========================================================
public sealed record PlatformPlanTranslationItem(string Language, string Name, string? Description);
