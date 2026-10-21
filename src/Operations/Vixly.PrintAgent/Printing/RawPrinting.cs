using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: RawPrinting.cs
//
// PURPOSE:
// Writes raw bytes to a Windows printer through the print spooler (Slice 3B).
//
// RESPONSIBILITIES:
// - Open the printer, start a RAW document and page, write, close
//
// IMPORTANT NOTES:
// - winspool.drv; every handle is closed, also on failure
// =========================================================
[SupportedOSPlatform("windows")]
internal static class RawPrinting
{
    public static string? Send(string printerName, byte[] data)
    {
        if (!NativeMethods.OpenPrinter(printerName, out IntPtr printer, IntPtr.Zero))
        {
            return "printer_not_installed";
        }

        try
        {
            DocInfo document = new() { DocumentName = "Vixly label", DataType = "RAW" };

            if (!NativeMethods.StartDocPrinter(printer, 1, document))
            {
                return "printer_refused";
            }

            try
            {
                if (!NativeMethods.StartPagePrinter(printer))
                {
                    return "printer_refused";
                }

                IntPtr buffer = Marshal.AllocCoTaskMem(data.Length);

                try
                {
                    Marshal.Copy(data, 0, buffer, data.Length);
                    bool written = NativeMethods.WritePrinter(printer, buffer, data.Length, out int count) && count == data.Length;
                    NativeMethods.EndPagePrinter(printer);
                    return written ? null : "printer_write_failed";
                }
                finally
                {
                    Marshal.FreeCoTaskMem(buffer);
                }
            }
            finally
            {
                NativeMethods.EndDocPrinter(printer);
            }
        }
        finally
        {
            NativeMethods.ClosePrinter(printer);
        }
    }
}
