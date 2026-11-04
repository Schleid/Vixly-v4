namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: ISettingsStore.cs
//
// PURPOSE:
// Where the phone keeps its settings (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Load and save the settings
//
// IMPORTANT NOTES:
// - The app keeps them in the phone's secure storage; the tests in memory
// =========================================================
public interface ISettingsStore
{
    Task<PrintHelperSettings> LoadAsync();

    Task SaveAsync(PrintHelperSettings settings);
}
