namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: PrintHelperSettings.cs
//
// PURPOSE:
// What the phone remembers once it joined a warehouse (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - The company's address, the print program key, the warehouse and the printer chosen
//
// IMPORTANT NOTES:
// - Kept in the phone's secure storage (Keychain, Android Keystore) by the app
// - No key: not joined yet
// =========================================================
public sealed record PrintHelperSettings(Uri? Site, string? AgentKey, string? Warehouse, string? PrinterName, string? PrinterAddress, int Dpi, string Media)
{
    public static PrintHelperSettings Empty { get; } = new(null, null, null, null, null, 203, "id");

    public bool IsJoined => Site is not null && !string.IsNullOrEmpty(AgentKey);

    public bool HasPrinter => IsJoined && !string.IsNullOrEmpty(PrinterAddress);
}
