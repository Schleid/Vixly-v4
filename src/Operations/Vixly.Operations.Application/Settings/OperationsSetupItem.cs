namespace Vixly.Operations.Application.Settings;

// =========================================================
// FILE: OperationsSetupItem.cs
//
// PURPOSE:
// One step of the setup checklist (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the step code, whether it is done and where to do it
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public sealed record OperationsSetupItem(string Code, bool Done, string Link);
