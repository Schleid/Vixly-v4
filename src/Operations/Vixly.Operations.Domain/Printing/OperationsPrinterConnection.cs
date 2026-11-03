namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsPrinterConnection.cs
//
// PURPOSE:
// How the dock print program reaches a label printer (Slice 3B).
//
// RESPONSIBILITIES:
// - Name the kinds of connection
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum OperationsPrinterConnection
{
    // A printer on the network, raw port (usually 9100).
    Network = 1,

    // A printer installed on the computer that runs the print program (USB or Bluetooth), by its Windows name.
    Installed = 2,

    // Slice 12E (owner H-6): a portable printer next to a phone running Vixly Print; the address is the phone's name for it.
    Bluetooth = 3
}
