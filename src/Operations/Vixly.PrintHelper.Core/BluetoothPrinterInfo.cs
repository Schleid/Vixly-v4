namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: BluetoothPrinterInfo.cs
//
// PURPOSE:
// A printer the phone sees nearby (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry its name as it shows itself and the phone's identifier of it
//
// IMPORTANT NOTES:
// - The identifier is what Operations keeps as the printer's address
// =========================================================
public sealed record BluetoothPrinterInfo(string Name, string Address);
