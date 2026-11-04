using System.Runtime.InteropServices;
using System.Text;

namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: InstalledLabelPrinter.cs
//
// PURPOSE:
// Sends labels to a printer installed on this Windows computer (USB or Bluetooth) as raw data (Slice 3B).
//
// RESPONSIBILITIES:
// - Open the printer by its Windows name and write the ZPL as one raw document
//
// IMPORTANT NOTES:
// - Windows only; elsewhere the job fails with installed_needs_windows
// - Raw: the Windows driver passes the ZPL to the printer unchanged
// =========================================================
public sealed class InstalledLabelPrinter : ILabelPrinterPort
{
    public string Connection => "installed";

    public Task<string?> SendAsync(string address, string payload, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<string?>("installed_needs_windows");
        }

        return Task.FromResult(RawPrinting.Send(address, Encoding.UTF8.GetBytes(payload)));
    }
}
