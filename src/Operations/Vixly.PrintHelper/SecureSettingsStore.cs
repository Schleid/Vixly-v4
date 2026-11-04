using System.Text.Json;
using Vixly.PrintHelper.Core;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: SecureSettingsStore.cs
//
// PURPOSE:
// Keeps the print helper's settings in the phone's secure storage (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Load and save the settings as JSON in SecureStorage (Keychain on iPhone, Keystore on Android)
//
// IMPORTANT NOTES:
// - The print program key never leaves secure storage
// =========================================================
public sealed class SecureSettingsStore : ISettingsStore
{
    private const string Key = "print-helper-settings";

    public async Task<PrintHelperSettings> LoadAsync()
    {
        try
        {
            string? json = await SecureStorage.Default.GetAsync(Key);
            return json is null ? PrintHelperSettings.Empty : JsonSerializer.Deserialize<PrintHelperSettings>(json) ?? PrintHelperSettings.Empty;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return PrintHelperSettings.Empty;
        }
    }

    public Task SaveAsync(PrintHelperSettings settings) => SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(settings));
}
