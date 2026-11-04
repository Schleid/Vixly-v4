namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsRoleNames.cs
//
// PURPOSE:
// The name of a standard role in a language, for the company's first copy of it (owner, 2026-10-29).
//
// RESPONSIBILITIES:
// - Give the name of a standard role key in en, fr or es
//
// IMPORTANT NOTES:
// - The texts live in OperationsText (ui.roles.standard.{key}); the company can rename the role afterwards
// =========================================================
public interface IOperationsRoleNames
{
    string Name(string key, string language);
}
