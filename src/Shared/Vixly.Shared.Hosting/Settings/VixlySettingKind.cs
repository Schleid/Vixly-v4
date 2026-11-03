namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingKind.cs
//
// PURPOSE:
// What a setting must look like (settings check, 2026-10-23).
//
// RESPONSIBILITIES:
// - Name each shape the check knows
//
// IMPORTANT NOTES:
// - Text: present; the others also check the shape
// =========================================================
public enum VixlySettingKind
{
    Text = 0,
    Url = 1,
    Email = 2,
    Folder = 3,
    Secret = 4,
    EcPrivateKey = 5,
    List = 6,
    Section = 7,
    Template = 8
}
