namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: IBluetoothPrinter.cs
//
// PURPOSE:
// The phone's Bluetooth, as the print helper uses it (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - List the printers nearby; send a label (ZPL) to one
//
// IMPORTANT NOTES:
// - Implemented by the app on each system; null from SendAsync means printed, otherwise an error code
// =========================================================
public interface IBluetoothPrinter
{
    Task<IReadOnlyList<BluetoothPrinterInfo>> ScanAsync(TimeSpan duration, CancellationToken cancellationToken);

    Task<string?> SendAsync(string address, string payload, CancellationToken cancellationToken);
}
