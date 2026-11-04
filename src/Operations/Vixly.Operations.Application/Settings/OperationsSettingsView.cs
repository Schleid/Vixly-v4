using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Settings;

// =========================================================
// FILE: OperationsSettingsView.cs
//
// PURPOSE:
// The settings as a page shows them (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the values and whether they were ever saved
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsSettingsView(OperationsTenantSettings Settings, bool Saved, bool CurrencyLocked, bool CanEdit);
