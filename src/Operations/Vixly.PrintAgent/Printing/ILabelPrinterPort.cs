namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: ILabelPrinterPort.cs
//
// PURPOSE:
// One way of reaching a label printer (Slice 3B).
//
// RESPONSIBILITIES:
// - Send printer commands to the printer at an address
//
// IMPORTANT NOTES:
// - Returns null when printed, or a short error code
// =========================================================
public interface ILabelPrinterPort
{
    // "network" or "installed", as Operations names the connection.
    string Connection { get; }

    Task<string?> SendAsync(string address, string payload, CancellationToken cancellationToken);
}
